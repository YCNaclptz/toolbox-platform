using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Auth.Api.Data;
using Auth.Api.Models;
using Auth.Api.Services;
using Platform.Shared.Auth;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthDbContext db, IConfiguration config, ILogger<AuthController> logger, AvatarStorageService avatarStorage) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);
        
        if (user is null)
        {
            logger.LogWarning("Login failed: user '{Username}' not found", request.Username);
            return Unauthorized(new { message = "帳號或密碼錯誤" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed: invalid password for user '{Username}'", request.Username);
            return Unauthorized(new { message = "帳號或密碼錯誤" });
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login failed: user '{Username}' is inactive", request.Username);
            return Unauthorized(new { message = "帳號已停用" });
        }

        // Get user's accessible apps
        var userAppIds = await db.UserApplicationAccesses
            .Where(ua => ua.UserId == user.Id)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        var apps = await db.Applications
            .Where(a => a.IsActive && userAppIds.Contains(a.Id))
            .OrderBy(a => a.SortOrder)
            .Select(a => new AppInfo(a.Id, a.Name, a.Icon ?? "", a.RoutePrefix))
            .ToListAsync();

        // Generate tokens
        var refreshToken = JwtHelper.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var accessToken = JwtHelper.GenerateToken(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role,
            user.MustChangePassword,
            userAppIds,
            config);

        logger.LogInformation("Login succeeded for user '{Username}' (role: {Role})", request.Username, user.Role);
        return Ok(new LoginResponse(accessToken, refreshToken, user.DisplayName, user.Role, user.MustChangePassword, apps, user.AvatarUrl));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "無效的刷新令牌" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "帳號已停用" });
        }

        // Get user's accessible apps
        var userAppIds = await db.UserApplicationAccesses
            .Where(ua => ua.UserId == user.Id)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        // Generate new tokens
        var newRefreshToken = JwtHelper.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        var accessToken = JwtHelper.GenerateToken(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role,
            user.MustChangePassword,
            userAppIds,
            config);

        return Ok(new TokenResponse(accessToken, newRefreshToken));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "目前密碼不正確" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();

        // Get user's accessible apps
        var userAppIds = await db.UserApplicationAccesses
            .Where(ua => ua.UserId == user.Id)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        var newToken = JwtHelper.GenerateToken(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role,
            user.MustChangePassword,
            userAppIds,
            config);

        return Ok(new ChangePasswordResponse(newToken, "密碼修改成功"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(user.Id, user.Username, user.DisplayName, user.Email, user.AvatarUrl, user.Role, user.CreatedAt));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(new { message = "顯示名稱不能為空" });

        if (request.DisplayName.Length > 100)
            return BadRequest(new { message = "顯示名稱不能超過 100 個字元" });

        if (request.Email is not null && request.Email.Length > 255)
            return BadRequest(new { message = "Email 不能超過 255 個字元" });

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Re-issue token with updated displayName
        var userAppIds = await db.UserApplicationAccesses
            .Where(ua => ua.UserId == user.Id)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        var newToken = JwtHelper.GenerateToken(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Role,
            user.MustChangePassword,
            userAppIds,
            config);

        return Ok(new { token = newToken, displayName = user.DisplayName, email = user.Email, avatarUrl = user.AvatarUrl, message = "個人資料更新成功" });
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        if (file.Length == 0)
            return BadRequest(new { message = "請選擇檔案" });

        try
        {
            // Delete old avatar if exists
            await avatarStorage.DeleteAvatarAsync(user.AvatarUrl);

            using var stream = file.OpenReadStream();
            var avatarUrl = await avatarStorage.UploadAvatarAsync(userId, stream, file.ContentType);

            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Ok(new { avatarUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        await avatarStorage.DeleteAvatarAsync(user.AvatarUrl);

        user.AvatarUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "頭像已移除" });
    }

    [Authorize]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var currentRole = User.GetRole();
        if (currentRole != "Admin")
            return Forbid();

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "使用者名稱已存在" });

        if (request.Role != "Admin" && request.Role != "User")
            return BadRequest(new { message = "角色必須為 Admin 或 User" });

        var user = new User
        {
            Username = request.Username,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            MustChangePassword = false,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("", new UserResponse(user.Id, user.Username, user.DisplayName, user.Role, user.MustChangePassword, user.IsActive));
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var currentRole = User.GetRole();
        if (currentRole != "Admin")
            return Forbid();

        var users = await db.Users
            .Select(u => new UserResponse(u.Id, u.Username, u.DisplayName, u.Role, u.MustChangePassword, u.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("apps")]
    public async Task<IActionResult> GetApplications()
    {
        var currentRole = User.GetRole();
        if (currentRole != "Admin")
            return Forbid();

        var apps = await db.Applications
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        return Ok(apps);
    }

    [Authorize]
    [HttpGet("me/apps")]
    public async Task<IActionResult> GetMyApps()
    {
        var userId = User.GetUserId();
        
        var apps = await db.UserApplicationAccesses
            .Where(ua => ua.UserId == userId)
            .Join(db.Applications, ua => ua.ApplicationId, a => a.Id, (ua, a) => a)
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .Select(a => new AppInfo(a.Id, a.Name, a.Icon ?? "", a.RoutePrefix))
            .ToListAsync();

        return Ok(apps);
    }

    [Authorize]
    [HttpPost("access")]
    public async Task<IActionResult> GrantAccess([FromBody] GrantAccessRequest request)
    {
        var currentRole = User.GetRole();
        if (currentRole != "Admin")
            return Forbid();

        var user = await db.Users.FindAsync(request.UserId);
        if (user is null)
            return NotFound(new { message = "使用者不存在" });

        var app = await db.Applications.FindAsync(request.ApplicationId);
        if (app is null)
            return NotFound(new { message = "應用程式不存在" });

        if (await db.UserApplicationAccesses.AnyAsync(ua => ua.UserId == request.UserId && ua.ApplicationId == request.ApplicationId))
            return BadRequest(new { message = "使用者已有此應用程式的存取權限" });

        var access = new UserApplicationAccess
        {
            UserId = request.UserId,
            ApplicationId = request.ApplicationId,
            GrantedBy = User.GetUserId(),
            GrantedAt = DateTime.UtcNow
        };

        db.UserApplicationAccesses.Add(access);
        await db.SaveChangesAsync();

        return Ok(new { message = "存取權限已授予" });
    }

    [Authorize]
    [HttpDelete("access/{userId}/{appId}")]
    public async Task<IActionResult> RevokeAccess(int userId, string appId)
    {
        var currentRole = User.GetRole();
        if (currentRole != "Admin")
            return Forbid();

        var access = await db.UserApplicationAccesses
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.ApplicationId == appId);

        if (access is null)
            return NotFound(new { message = "存取權限不存在" });

        db.UserApplicationAccesses.Remove(access);
        await db.SaveChangesAsync();

        return Ok(new { message = "存取權限已撤銷" });
    }
}