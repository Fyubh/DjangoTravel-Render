using System.ComponentModel.DataAnnotations.Schema;

namespace Jango_Travel.Models;

public class Trip
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionHtml { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int CityId { get; set; }         // ✅ обязателен
    public City City { get; set; } = null!; // навигация
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}