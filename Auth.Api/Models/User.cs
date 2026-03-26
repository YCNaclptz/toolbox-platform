using Platform.Shared.Data;

namespace Auth.Api.Models;

public class User : BaseEntity
{
    public required string Username { get; set; }
    public string? Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = "User";
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
