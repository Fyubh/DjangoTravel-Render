namespace Jango_Travel.Models;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
    
    // Геолокация
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}