namespace Jango_Travel.Models;

public class Photo
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;
}