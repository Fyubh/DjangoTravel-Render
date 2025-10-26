namespace Jango_Travel.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jango_Travel.Data;
using Jango_Travel.Models;

public class TripsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TripsController(ApplicationDbContext context)
    {
        _context = context;
    }
    

    public async Task<IActionResult> Index()
    {
// ✅ Подсчёт уникальных городов по всем путешествиям
        var visitedCities = await _context.Trips
            .Include(t => t.City)
            .Where(t => t.City != null)
            .Select(t => t.City!.Name)
            .Distinct()
            .CountAsync();

        ViewData["VisitedCities"] = visitedCities;
        ViewData["VisitedCities"] = visitedCities;
        ViewData["VisitedCities"] = visitedCities;
        // Загружаем все путешествия
        var trips = _context.Trips
            .Include(t => t.City)
            .ThenInclude(c => c.Country)
            .OrderByDescending(t => t.StartDate);

        // 🔥 Подсчёт стран
        var visitedCountries = await _context.Trips
            .Include(t => t.City)
            .ThenInclude(c => c.Country)
            .Select(t => t.City.Country.Name)
            .Distinct()
            .CountAsync();

        // 🌍 Всего стран в мире (по ООН) — 195
        int totalCountries = 195;

        // Сколько осталось
        int remaining = totalCountries - visitedCountries;
        double percentVisited = (double)visitedCountries / totalCountries * 100;
        double percentRemaining = 100 - percentVisited;

        // Передаём в View
        ViewData["VisitedCountries"] = visitedCountries;
        ViewData["RemainingCountries"] = remaining;
        ViewData["PercentVisited"] = percentVisited;
        ViewData["PercentRemaining"] = percentRemaining;
        
        // === 1️⃣ Trips per Year ===
        var tripsByYear = await _context.Trips
            .GroupBy(t => t.StartDate.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .OrderBy(g => g.Year)
            .ToListAsync();
        ViewData["TripsByYear"] = tripsByYear;
        
// === 3️⃣ Total Distance (Haversine formula) ===
        double totalDistance = 0;
        var orderedTrips = await _context.Trips
            .Include(t => t.City)
            .OrderBy(t => t.StartDate)
            .ToListAsync();

        
        for (int i = 1; i < orderedTrips.Count; i++)
        {
            var prev = orderedTrips[i - 1].City;
            var curr = orderedTrips[i].City;

            if (prev != null && curr != null)
            {
                double R = 6371; // Earth radius (km)
                double dLat = (curr.Latitude - prev.Latitude) * Math.PI / 180;
                double dLon = (curr.Longitude - prev.Longitude) * Math.PI / 180;

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(prev.Latitude * Math.PI / 180) *
                           Math.Cos(curr.Latitude * Math.PI / 180) *
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                totalDistance += R * c;
            }
        }
        ViewData["TotalDistance"] = Math.Round(totalDistance, 1); // км
        ViewData["EarthPercent"] = Math.Round(totalDistance / 40075 * 100, 2); // 40075 км = длина экватора

        
        // === 5️⃣ Travel Streaks based on home cities ===
        var homeCities = new[] { "Prague", "Almaty", "Seoul" };
        var orderedTrips2 = await _context.Trips
            .Include(t => t.City)
            .OrderBy(t => t.StartDate)
            .ToListAsync();

        DateTime? streakStart = null;
        DateTime? lastTripDate = null;
        bool away = false;
        int longestStreakDays = 0;

        foreach (var trip in orderedTrips2)
        {
            var cityName = trip.City?.Name ?? "";

            if (homeCities.Contains(cityName))
            {
                // Вернулся домой → заканчиваем streak
                if (away && streakStart != null && lastTripDate != null)
                {
                    var daysAway = (lastTripDate.Value - streakStart.Value).TotalDays;
                    if (daysAway > longestStreakDays)
                        longestStreakDays = (int)daysAway;
                }
                away = false;
                streakStart = null;
            }
            else
            {
                // Уехал из дома
                if (!away)
                {
                    away = true;
                    streakStart = trip.StartDate;
                }
                lastTripDate = trip.EndDate;
            }
        }

        ViewData["LongestHomeBasedStreak"] = longestStreakDays;

        return View(await trips.ToListAsync());
    }
    
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var trip = await _context.Trips
            .Include(t => t.City)
            .ThenInclude(c => c.Country)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (trip == null)
            return NotFound();

        int visitCount = await _context.Trips
            .CountAsync(t => t.CityId == trip.CityId);

        ViewData["VisitCount"] = visitCount;

        return View(trip);
    }

}