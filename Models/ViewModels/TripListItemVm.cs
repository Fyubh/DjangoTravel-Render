namespace Jango_Travel.Models.ViewModels;

public class TripListItemVm
{
    public int TripId { get; set; }
    public string Title { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string CityName { get; set; } = "";
    public string CountryName { get; set; } = "";
    
    public bool HasArticle { get; set; }   // ← НОВОЕ
}