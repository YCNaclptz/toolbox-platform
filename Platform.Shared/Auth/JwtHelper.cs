using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Shared.Auth;

public static class JwtHelper
{
    public static string GenerateToken(
        int userId,
        string username,
        string displayName,
        string role,
        bool mustChangePassword,
        IEnumerable<string> accessibleApps,
        IConfiguration config)
    {
        var key = config["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY")
            ?? throw new InvalidOperationException("JWT key not configured");
        var issuer = config["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "Platform.Api";
        var audience = config["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "Platform.Frontend";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("username", username),
            new Claim("displayName", displayName),
            new Claim("role", role),
            new Claim("mustChangePassword", mustChangePassword.ToString().ToLower()),
            new Claim("apps", string.Join(",", accessibleApps))
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),  // 30 minutes access token
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JsonWebTokenHandler();
        return tokenHandler.CreateToken(tokenDescriptor);
    }

    public static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
