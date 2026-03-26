namespace Auth.Api.Models;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string RefreshToken, string DisplayName, string Role, bool MustChangePassword, List<AppInfo> Apps, string? AvatarUrl);

public record AppInfo(string Id, string Name, string Icon, string RoutePrefix);

public record RefreshTokenRequest(string RefreshToken);

public record TokenResponse(string Token, string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangePasswordResponse(string Token, string Message);

public record RegisterRequest(string Username, string Password, string DisplayName, string Role);

public record UserResponse(int Id, string Username, string DisplayName, string Role, bool MustChangePassword, bool IsActive);

public record GrantAccessRequest(int UserId, string ApplicationId);

public record UserProfileResponse(int Id, string Username, string DisplayName, string? Email, string? AvatarUrl, string Role, DateTime CreatedAt);

public record UpdateProfileRequest(string DisplayName, string? Email);
