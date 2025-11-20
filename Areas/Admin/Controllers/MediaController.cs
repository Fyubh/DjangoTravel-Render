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

    // **********************
    //   Папка на диске Render
    // **********************
    private const string UploadRoot = "/data/uploads";

    private string GetTripFolder(int tripId)
        => Path.Combine(UploadRoot, tripId.ToString(CultureInfo.InvariantCulture));

    private static string WebPath(int tripId, string fileName)
        => $"/uploads/{tripId}/{fileName}";

    // /Admin/Media?tripId=123
    public IActionResult Index(int? tripId)
    {
        ViewBag.TripId = tripId;

        if (tripId is null)
        {
            ViewBag.Files = Array.Empty<(string file, string url)>();
            return View();
        }

        var dir = GetTripFolder(tripId.Value);
        Directory.CreateDirectory(dir);

        // Папка WWWROOT нужна для отдачи статических файлов
        var staticDir = Path.Combine(_env.WebRootPath, "uploads", tripId.ToString());
        Directory.CreateDirectory(staticDir);

        var files = Directory.EnumerateFiles(dir)
            .OrderByDescending(p => System.IO.File.GetCreationTimeUtc(p))
            .Select(p =>
            {
                var fileName = Path.GetFileName(p);

                // Синхронизируем файл в wwwroot
                var staticPath = Path.Combine(staticDir, fileName);
                if (!System.IO.File.Exists(staticPath))
                    System.IO.File.Copy(p, staticPath);

                return (file: fileName, url: WebPath(tripId.Value, fileName));
            })
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

        var staticDir = Path.Combine(_env.WebRootPath, "uploads", tripId.ToString());
        Directory.CreateDirectory(staticDir);

        var saved = new List<string>();

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;

            var ext = Path.GetExtension(file.FileName);
            var name = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";

            var diskPath = Path.Combine(dir, name);       // сохраняем на /data/
            var staticPath = Path.Combine(staticDir, name); // копия в wwwroot

            await using (var stream = System.IO.File.Create(diskPath))
                await file.CopyToAsync(stream);

            // Копия в статическую папку
            System.IO.File.Copy(diskPath, staticPath);

            saved.Add(WebPath(tripId, name));
        }

        TempData["Success"] = $"{saved.Count} файлов загружено.";
        return RedirectToAction(nameof(Index), new { tripId });
    }

    // /Admin/Media/All
    public IActionResult All()
    {
        var root = UploadRoot;

        if (!Directory.Exists(root))
        {
            ViewBag.Items = Array.Empty<(int tripId, string file, string url)>();
            return View();
        }

        var items = Directory.EnumerateDirectories(root)
            .SelectMany(dir =>
            {
                var tripIdStr = Path.GetFileName(dir);
                if (!int.TryParse(tripIdStr, out var tripId))
                    return Enumerable.Empty<(int, string, string)>();

                return Directory.EnumerateFiles(dir)
                    .OrderByDescending(p => System.IO.File.GetCreationTimeUtc(p))
                    .Select(p =>
                    {
                        var fileName = Path.GetFileName(p);
                        return (tripId, fileName, WebPath(tripId, fileName));
                    });
            })
            .ToList();

        ViewBag.Items = items;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int tripId, string file)
    {
        var diskPath = Path.Combine(GetTripFolder(tripId), file);
        var staticPath = Path.Combine(_env.WebRootPath, "uploads", tripId.ToString(), file);

        if (System.IO.File.Exists(diskPath))
            System.IO.File.Delete(diskPath);

        if (System.IO.File.Exists(staticPath))
            System.IO.File.Delete(staticPath);

        TempData["Success"] = "Файл удалён.";
        return RedirectToAction(nameof(Index), new { tripId });
    }
}
