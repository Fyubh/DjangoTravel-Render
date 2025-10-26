namespace Jango_Travel.Models.ViewModels;

public class VisitedCountryVm
{
    public int CountryId { get; set; }
    public string CountryName { get; set; } = "";
    public int CitiesCount { get; set; }
    public int TripsCount { get; set; }
}