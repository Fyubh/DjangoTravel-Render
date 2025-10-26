using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Jango_Travel.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MediaController : Controller
{
    private readonly IWebHostEnvironment _env;

    public MediaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string GetTripFolder(int tripId)
        => Path.Combine(_env.WebRootPath, "uploads", tripId.ToString(CultureInfo.InvariantCulture));

    private static string WebPath(int tripId, string fileName)
        => $"/uploads/{tripId}/{fileName}";

    // /Admin/Media?tripId=123 — страница загрузки/просмотра фото для конкретного Trip
    public IActionResult Index(int? tripId)
    {
        ViewBag.TripId = tripId;

        if (tripId is null)
        {
            ViewBag.Files = Array.Empty<(string file, string url)>();
            return View();
        }

        var dir = GetTripFolder(tripId.Value);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var files = Directory.EnumerateFiles(dir)
            .OrderByDescending(p => System.IO.File.GetCreationTimeUtc(p))
            .Select(p => (file: Path.GetFileName(p), url: WebPath(tripId.Value, Path.GetFileName(p))))
            .ToList();

        ViewBag.Files = files;
        return View();
    }

    // POST: /Admin/Media/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int tripId, List<IFormFile> files)
    {
        if (tripId <= 0 || files == null || files.Count == 0)
        {
            TempData["Error"] = "Укажите Trip и выберите файлы.";
            return RedirectToAction(nameof(Index), new { tripId });
        }

        var dir = GetTripFolder(tripId);
        Directory.CreateDirectory(dir);

        var saved = new List<string>();

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;

            // уникальное имя: yyyyMMdd_HHmmss_guid.ext
            var ext = Path.GetExtension(file.FileName);
            var name = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(dir, name);

            await using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);

            saved.Add(WebPath(tripId, name));
        }

        // Подсказываем готовые HTML-сниппеты
        TempData["Success"] = $"Загружено: {saved.Count}. Примеры HTML:\n" +
                              string.Join("\n", saved.Select(u => $"<img src=\"{u}\" alt=\"\">"));

        return RedirectToAction(nameof(Index), new { tripId });
    }

    // /Admin/Media/All — галерея всех фото
    public IActionResult All()
    {
        var root = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(root))
        {
            ViewBag.Items = Array.Empty<(int tripId, string file, string url)>();
            return View();
        }

        var items = Directory.EnumerateDirectories(root)
            .SelectMany(dir =>
            {
                var tripIdStr = Path.GetFileName(dir);
                if (!int.TryParse(tripIdStr, out var tripId)) return Enumerable.Empty<(int, string, string)>();

                return Directory.EnumerateFiles(dir)
                    .OrderByDescending(p => System.IO.File.GetCreationTimeUtc(p))
                    .Select(p => (tripId, file: Path.GetFileName(p), url: WebPath(tripId, Path.GetFileName(p))));
            })
            .ToList();

        ViewBag.Items = items;
        return View();
    }

    // Опционально: удалить файл
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int tripId, string file)
    {
        var full = Path.Combine(GetTripFolder(tripId), file);
        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);

        TempData["Success"] = "Файл удалён.";
        return RedirectToAction(nameof(Index), new { tripId });
    }
}
