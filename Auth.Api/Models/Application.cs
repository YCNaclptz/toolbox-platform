namespace Auth.Api.Models;

public class Application
{
    public required string Id { get; set; }  // e.g. "swear-jar"
    public required string Name { get; set; }  // e.g. "髒話罐"
    public string? Description { get; set; }
    public string? Icon { get; set; }  // CSS class name for the icon (e.g. "icon-money-bag")
    public required string RoutePrefix { get; set; }  // e.g. "/swear-jar"
    public required string ApiPrefix { get; set; }  // e.g. "/api/swear-jar"
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
