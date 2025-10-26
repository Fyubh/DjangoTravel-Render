namespace Jango_Travel.Models.ViewModels;

public class VisitedCityVm
{
    public int CityId { get; set; }
    public string CityName { get; set; } = "";
    public int TripsCount { get; set; }
    public string CountryName { get; set; } = "";
    public int CountryId { get; set; }
}