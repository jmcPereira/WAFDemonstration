namespace VulnerableApp.Models;

public record Message
{
    public int Id { get; set; }
    public string Owner { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}