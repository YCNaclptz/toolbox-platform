namespace Auth.Api.Models;

public class UserApplicationAccess
{
    public int UserId { get; set; }
    public required string ApplicationId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public int? GrantedBy { get; set; }
    public User? User { get; set; }
    public Application? Application { get; set; }
}
