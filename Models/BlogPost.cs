namespace Jango_Travel.Models;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;
}