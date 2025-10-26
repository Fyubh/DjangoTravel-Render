using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jango_Travel.Data;
using Jango_Travel.Models;
using Microsoft.AspNetCore.Mvc.Rendering; 

namespace Jango_Travel.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔐 Простая авторизация по секретному ключу (только для тебя)
        private bool IsAuthorized()
        {
            var key = HttpContext.Request.Cookies["AdminKey"];
            return key == "3pjwlNLENYRiV2WA"; // поменяй на свой ключ!
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string password)
        {
            if (password == "3pjwlNLENYRiV2WA") // поставь свой пароль
            {
                Response.Cookies.Append("AdminKey", "3pjwlNLENYRiV2WA", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true
                });
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Invalid password.";
            return View();
        }

        // Главная страница панели
        public async Task<IActionResult> Index()
        {
            if (!IsAuthorized()) return RedirectToAction("Login");

            var trips = await _context.Trips
                .Include(t => t.City)
                .ThenInclude(c => c.Country)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return View(trips);
        }

        // Добавление путешествия
        public async Task<IActionResult> Add()
        {
            if (!IsAuthorized()) return RedirectToAction("Login");

            ViewBag.Cities = new SelectList(
                await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name");
            return View(new Trip { StartDate = DateTime.Today, EndDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("Title,Description,StartDate,EndDate,CityId")] Trip form)
        {
            if (!IsAuthorized()) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                ViewBag.Cities = new SelectList(
                    await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", form.CityId);
                return View(form);
            }

            _context.Trips.Add(form);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // Редактирование
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAuthorized()) return RedirectToAction("Login");

            var trip = await _context.Trips
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            ViewBag.Cities = new SelectList(
                await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", trip.CityId);

            return View(trip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Title,Description,StartDate,EndDate,CityId")] Trip form)
        {
            if (!IsAuthorized()) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                ViewBag.Cities = new SelectList(
                    await _context.Cities.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", form.CityId);
                return View(form);
            }

            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == form.Id);
            if (trip == null) return NotFound();

            // ✅ точечно обновляем поля
            trip.Title = form.Title;
            trip.Description = form.Description;
            trip.StartDate = form.StartDate;
            trip.EndDate = form.EndDate;
            trip.CityId = form.CityId;    // главное — сохранить связь с городом

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        // Добавление фото и статьи
        [HttpGet]
        public async Task<IActionResult> AddMedia(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            return View(trip);
        }

        [HttpPost]
        public async Task<IActionResult> AddMedia(int id, IFormFile? mediaFile, string? article)
        {
            var trip = await _context.Trips
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null)
                return NotFound();

            // 📷 сохраняем фото
            if (mediaFile != null && mediaFile.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(mediaFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await mediaFile.CopyToAsync(stream);
                }

                var photo = new Photo { Url = "/uploads/" + fileName, Trip = trip };
                trip.Photos.Add(photo);
            }

            // 📝 сохраняем статью
            if (!string.IsNullOrWhiteSpace(article))
            {
                trip.Description += "\n\n📖 " + article.Trim();
            }

            _context.Update(trip);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Trips", new { id = trip.Id });
        }

    }
}
