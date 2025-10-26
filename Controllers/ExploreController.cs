using Jango_Travel.Data;
using Jango_Travel.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jango_Travel.Controllers;

public class ExploreController : Controller
{
    private readonly ApplicationDbContext _db;
    public ExploreController(ApplicationDbContext db) => _db = db;

    // /Explore  → список стран, где были трипы
    public async Task<IActionResult> Index()
    {
        var countries = await _db.Trips
            .Include(t => t.City)!.ThenInclude(c => c!.Country)
            .GroupBy(t => new { CountryId = t.City!.CountryId, CountryName = t.City!.Country!.Name })
            .Select(g => new VisitedCountryVm
            {
                CountryId   = g.Key.CountryId,
                CountryName = g.Key.CountryName,
                CitiesCount = g.Select(t => t.CityId).Distinct().Count(),
                TripsCount  = g.Count()
            })
            .OrderBy(x => x.CountryName)
            .ToListAsync();

        return View(countries);
    }

    // /Explore/Country/5 → города этой страны с количеством поездок
    public async Task<IActionResult> Country(int id)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == id);
        if (country == null) return NotFound();

        var cities = await _db.Trips
            .Include(t => t.City)
            .Where(t => t.City!.CountryId == id)
            .GroupBy(t => new { t.CityId, CityName = t.City!.Name })
            .Select(g => new VisitedCityVm
            {
                CityId       = g.Key.CityId,
                CityName     = g.Key.CityName,
                TripsCount   = g.Count(),
                CountryId    = id,
                CountryName  = country.Name
            })
            .OrderBy(x => x.CityName)
            .ToListAsync();

        ViewData["CountryName"] = country.Name;
        return View(cities);
    }

    // /Explore/City/12 → список поездок в город
    public async Task<IActionResult> City(int id)
    {
        var city = await _db.Cities.Include(c => c.Country).FirstOrDefaultAsync(c => c.Id == id);
        if (city == null) return NotFound();

        var trips = await _db.Trips
            .Where(t => t.CityId == id)
            .OrderByDescending(t => t.StartDate)
            .Select(t => new TripListItemVm
            {
                TripId      = t.Id,
                Title       = t.Title,
                StartDate   = t.StartDate,
                EndDate     = t.EndDate,
                CityName    = city.Name,
                CountryName = city.Country!.Name,
                HasArticle  = !string.IsNullOrEmpty(t.DescriptionHtml)  // ← вот это
            })
            .ToListAsync();

        ViewData["CityName"]    = city.Name;
        ViewData["CountryName"] = city.Country!.Name;
        ViewData["CountryId"]   = city.CountryId;

        return View(trips);
    }

}
