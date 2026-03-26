using Platform.Shared.Data;

namespace SwearJar.Api.Models;

public class SwearEntry : BaseEntity
{
    public int UserId { get; set; }
    public DateTime Time { get; set; }
    public required string Reason { get; set; }
    public int Fine { get; set; }
    public bool IsDeleted { get; set; }
}

