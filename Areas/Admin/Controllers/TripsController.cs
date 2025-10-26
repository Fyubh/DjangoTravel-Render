using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jango_Travel.Data;
using Jango_Travel.Models;
using Jango_Travel.Models.ViewModels;
using Jango_Travel.Utils;


namespace Jango_Travel.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TripsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    public TripsController(ApplicationDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    // LIST + поиск
    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Trips
            .Include(t => t.City).ThenInclude(c => c.Country)
            .OrderByDescending(t => t.StartDate).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Title.Contains(q) || (t.City != null && t.City.Name.Contains(q)));

        return View(await query.ToListAsync());
    }

    // CREATE
    public async Task<IActionResult> Create()
    {
        ViewBag.Cities = await _db.Cities.Include(c => c.Country)
                           .OrderBy(c => c.Name).ToListAsync();
        return View(new TripFormVm { StartDate = DateTime.Today, EndDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TripFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Cities = await _db.Cities.Include(c => c.Country).OrderBy(c => c.Name).ToListAsync();
            return View(vm);
        }
        
        

        City? city = null;

        if (vm.CityId.HasValue)
        {
            city = await _db.Cities.FindAsync(vm.CityId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(vm.NewCityName) && !string.IsNullOrWhiteSpace(vm.NewCountryName))
        {
            var country = await _db.Countries.FirstOrDefaultAsync(c => c.Name == vm.NewCountryName)
                          ?? new Country { Name = vm.NewCountryName, Code = vm.NewCountryName[..Math.Min(2, vm.NewCountryName.Length)].ToUpper() };

            if (country.Id == 0) _db.Countries.Add(country);

            city = new City
            {
                Name = vm.NewCityName!,
                Latitude = vm.NewCityLat ?? 0,
                Longitude = vm.NewCityLon ?? 0,
                Country = country
            };
            _db.Cities.Add(city);
        }

        // после логики создания/поиска города:
        int cityId;

        if (vm.CityId.HasValue)
        {
            cityId = vm.CityId.Value;         // выбран существующий город из списка
        }
        else if (city != null)                // 'city' — это новый/найденный выше город
        {
            cityId = city.Id;
        }
        else
        {
            ModelState.AddModelError("CityId", "Укажите город.");
            // верни форму с ошибкой (и подгрузи справочники, если нужно)
            return View(vm);
        }

        var cleanHtml = string.IsNullOrWhiteSpace(vm.DescriptionHtml)
            ? null
            : HtmlSafe.Clean(vm.DescriptionHtml);  // не забудь using Jango_Travel.Utils;

        var trip = new Trip
        {
            Title = vm.Title,
            Description = null,
            DescriptionHtml = cleanHtml,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            CityId = cityId
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        await SaveFilesAsync(trip.Id, vm.Files);
        return RedirectToAction(nameof(Edit), new { id = trip.Id });
    }

    // EDIT
    // Areas/Admin/Controllers/TripsController.cs
    public async Task<IActionResult> Edit(int id)
    {
        var trip = await _db.Trips
            .Include(t => t.City).ThenInclude(c => c.Country)
            .Include(t => t.Photos)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null) return NotFound();

        ViewBag.Cities = await _db.Cities
            .Include(c => c.Country)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // НИКАКОЙ очистки и смены trip.DescriptionHtml здесь не делаем!
        return View(new TripFormVm
        {
            Id = trip.Id,
            Title = trip.Title,
            DescriptionHtml = trip.DescriptionHtml, // просто отдаём в форму
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            CityId = trip.CityId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TripFormVm vm)
    {
        if (id != vm.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Cities = await _db.Cities.Include(c => c.Country).OrderBy(c => c.Name).ToListAsync();
            return View(vm);
        }

        var trip = await _db.Trips.Include(t => t.Photos).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();

        // Очистка HTML — только в POST
        var cleanHtml = string.IsNullOrWhiteSpace(vm.DescriptionHtml) ? null : HtmlSafe.Clean(vm.DescriptionHtml);

        trip.Title = vm.Title;
        trip.StartDate = vm.StartDate;
        trip.EndDate = vm.EndDate;
        trip.CityId = vm.CityId ?? trip.CityId;
        trip.DescriptionHtml = cleanHtml;

        await _db.SaveChangesAsync();

        if (vm.Files?.Any() == true)
            await SaveFilesAsync(trip.Id, vm.Files);

        return RedirectToAction(nameof(Edit), new { id = trip.Id });
    }


    // DELETE TRIP
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _db.Trips.Include(t => t.Photos).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();

        // удаляем файлы
        var dir = Path.Combine(_env.WebRootPath, "uploads", trip.Id.ToString());
        if (Directory.Exists(dir)) Directory.Delete(dir, true);

        _db.Photos.RemoveRange(trip.Photos);
        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // DELETE PHOTO
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id, int tripId)
    {
        var photo = await _db.Photos.FindAsync(id);
        if (photo != null)
        {
            var full = Path.Combine(_env.WebRootPath, photo.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id = tripId });
    }

    // сохранение загруженных файлов
    private async Task SaveFilesAsync(int tripId, List<IFormFile> files)
    {
        if (files == null || files.Count == 0) return;

        var dir = Path.Combine(_env.WebRootPath, "uploads", tripId.ToString());
        Directory.CreateDirectory(dir);

        foreach (var f in files.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(f.FileName);
            var name = $"{Guid.NewGuid()}{ext}";
            var full = Path.Combine(dir, name);
            using (var stream = new FileStream(full, FileMode.Create))
                await f.CopyToAsync(stream);

            var url = $"/uploads/{tripId}/{name}";
            _db.Photos.Add(new Photo { TripId = tripId, Url = url });
        }
        await _db.SaveChangesAsync();
    }
}
