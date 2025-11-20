using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Jango_Travel.Data;
using Jango_Travel.Models;

var builder = WebApplication.CreateBuilder(args);

// --- DbContext ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(conn));   // тут НИЧЕГО особенного

builder.Services.AddControllersWithViews();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- Identity + Roles ---
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // можно входить сразу
        // при желании усили параметр Policy пароля:
        // options.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

var uploadsPath = "/data/uploads";
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// отдать /data/uploads по урлу /uploads
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// CSP под Tailwind/Leaflet/Chart.js (если надо — правь домены)
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.tailwindcss.com https://unpkg.com https://cdn.jsdelivr.net 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "object-src 'none'; " +
        "connect-src 'self' https://*.tile.openstreetmap.org https://tile.openstreetmap.org;";
    await next();
});

app.UseRouting();
app.UseAuthentication();       // <= ВАЖНО: до Authorization
app.UseAuthorization();

// --- Маршруты Areas и по умолчанию ---
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Trips}/{action=Index}/{id?}");

app.MapRazorPages();

// --- Папка для загрузок (медиа) ---
var uploadsRoot = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

// --- Сиды: Trips + Admin ---
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // ===== SEED TRIPS (оставил твою логику; при желании сократи) =====
    if (!db.Trips.Any())
    {
        var tripsData = new (string City, string Country, DateTime StartDate, double Lat, double Lon)[]
        {
            ("Prague", "Czech Republic", new DateTime(2022,9,2), 50.0755, 14.4378),
            ("Brussels", "Belgium", new DateTime(2022,10,8), 50.8503, 4.3517),
            ("Lille", "France", new DateTime(2022,10,8), 50.6292, 3.0573),
            ("Brussels", "Belgium", new DateTime(2022,10,9), 50.8503, 4.3517),
            ("Prague", "Czech Republic", new DateTime(2022,10,10), 50.0755, 14.4378),
            ("Pisek", "Czech Republic", new DateTime(2022,10,22), 49.3088, 14.1475),
            ("Prague", "Czech Republic", new DateTime(2022,10,22), 50.0755, 14.4378),
            ("Bratislava", "Slovakia", new DateTime(2022,10,29), 48.1486, 17.1077),
            ("Vienna", "Austria", new DateTime(2022,10,29), 48.2082, 16.3738),
            ("Prague", "Czech Republic", new DateTime(2022,10,31), 50.0755, 14.4378),
            ("Brno", "Czech Republic", new DateTime(2022,11,12), 49.1951, 16.6068),
            ("Prague", "Czech Republic", new DateTime(2022,11,15), 50.0755, 14.4378),
            ("Istanbul", "Turkey", new DateTime(2022,12,24), 41.0082, 28.9784),
            ("Almaty", "Kazakhstan", new DateTime(2022,12,25), 43.2389, 76.8897),
            ("Prague", "Czech Republic", new DateTime(2023,01,07), 50.0755, 14.4378),
            ("Turin", "Italy", new DateTime(2023,01,25), 45.0703, 7.6869),
            ("Genova", "Italy", new DateTime(2023,01,25), 44.4056, 8.9463),
            ("Florence", "Italy", new DateTime(2023,01,26), 43.7699, 11.2556),
            ("Milan", "Italy", new DateTime(2023,01,27), 45.4642, 9.1900),
            ("Turin", "Italy", new DateTime(2023,01,27), 45.0703, 7.6869),
            ("Prague", "Czech Republic", new DateTime(2023,01,28), 50.0755, 14.4378),
            ("Dresden", "Germany", new DateTime(2023,04,03), 51.0504, 13.7373),
            ("Prague", "Czech Republic", new DateTime(2023,04,03), 50.0755, 14.4378),
            ("Cesky Raj", "Czech Republic", new DateTime(2023,04,09), 50.5, 15.1667),
            ("Prague", "Czech Republic", new DateTime(2023,04,09), 50.0755, 14.4378),
            ("Karlstein", "Czech Republic", new DateTime(2023,04,17), 49.9379, 14.1898),
            ("Prague", "Czech Republic", new DateTime(2023,04,17), 50.0755, 14.4378),
            ("Vienna", "Austria", new DateTime(2023,05,23), 48.2082, 16.3738),
            ("Prague", "Czech Republic", new DateTime(2023,05,23), 50.0755, 14.4378),
            ("Istanbul", "Turkey", new DateTime(2023,05,30), 41.0082, 28.9784),
            ("Almaty", "Kazakhstan", new DateTime(2023,05,31), 43.2389, 76.8897),
            ("Oskemen", "Kazakhstan", new DateTime(2023,07,07), 49.9483, 82.6286),
            ("Privolnoye", "Kazakhstan", new DateTime(2023,07,08), 53.55, 73.1),
            ("Azovoe", "Kazakhstan", new DateTime(2023,07,08), 53.4, 73.2),
            ("Oskemen", "Kazakhstan", new DateTime(2023,07,10), 49.9483, 82.6286),
            ("Almaty", "Kazakhstan", new DateTime(2023,07,11), 43.2389, 76.8897),
            ("Akshi", "Kazakhstan", new DateTime(2023,07,16), 45.7167, 77.8833),
            ("Almaty", "Kazakhstan", new DateTime(2023,07,22), 43.2389, 76.8897),
            ("Istanbul", "Turkey", new DateTime(2023,09,14), 41.0082, 28.9784),
            ("Prague", "Czech Republic", new DateTime(2023,09,15), 50.0755, 14.4378),
            ("Geneva", "Switzerland", new DateTime(2023,09,22), 46.2044, 6.1432),
            ("Lyon", "France", new DateTime(2023,09,23), 45.764, 4.8357),
            ("Marseille", "France", new DateTime(2023,09,23), 43.2965, 5.3698),
            ("Nice", "France", new DateTime(2023,09,23), 43.7102, 7.262),
            ("Monaco", "Monaco", new DateTime(2023,09,23), 43.7384, 7.4246),
            ("Cannes", "France", new DateTime(2023,09,23), 43.5528, 7.0174),
            ("Milan", "Italy", new DateTime(2023,09,25), 45.4642, 9.19),
            ("Como", "Italy", new DateTime(2023,09,25), 45.8081, 9.0852),
            ("Prague", "Czech Republic", new DateTime(2023,09,26), 50.0755, 14.4378),
            ("Berlin", "Germany", new DateTime(2023,11,02), 52.52, 13.405),
            ("Prague", "Czech Republic", new DateTime(2023,11,02), 50.0755, 14.4378),
            ("Amsterdam", "Netherlands", new DateTime(2023,11,14), 52.3676, 4.9041),
            ("Paris", "France", new DateTime(2023,11,15), 48.8566, 2.3522),
            ("Brussels", "Belgium", new DateTime(2023,11,16), 50.8503, 4.3517),
            ("Prague", "Czech Republic", new DateTime(2023,11,16), 50.0755, 14.4378),
            ("Almaty", "Kazakhstan", new DateTime(2023,12,22), 43.2389, 76.8897),
            ("Istanbul", "Turkey", new DateTime(2024,01,04), 41.0082, 28.9784),
            ("Prague", "Czech Republic", new DateTime(2024,01,05), 50.0755, 14.4378),
            ("Vienna", "Austria", new DateTime(2024,01,31), 48.2082, 16.3738),
            ("Prague", "Czech Republic", new DateTime(2024,01,31), 50.0755, 14.4378),
            ("Paris", "France", new DateTime(2024,03,05), 48.8566, 2.3522),
            ("Prague", "Czech Republic", new DateTime(2024,03,06), 50.0755, 14.4378),
            ("Liberec", "Czech Republic", new DateTime(2024,05,31), 50.7671, 15.0562),
            ("Prague", "Czech Republic", new DateTime(2024,05,01), 50.0755, 14.4378),
            ("Kralupy-nad-Vltavou", "Czech Republic", new DateTime(2024,05,07), 50.241, 14.313),
            ("Slany", "Czech Republic", new DateTime(2024,05,07), 50.23, 14.08),
            ("Prague", "Czech Republic", new DateTime(2024,05,07), 50.0755, 14.4378),
            ("Olomouc", "Czech Republic", new DateTime(2024,05,16), 49.5938, 17.2509),
            ("Ostrava", "Czech Republic", new DateTime(2024,05,16), 49.8209, 18.2625),
            ("Prague", "Czech Republic", new DateTime(2024,05,16), 50.0755, 14.4378),
            ("Vienna", "Austria", new DateTime(2024,05,24), 48.2082, 16.3738),
            ("Nice", "France", new DateTime(2024,05,25), 43.7102, 7.262),
            ("Cannes", "France", new DateTime(2024,05,25), 43.5528, 7.0174),
            ("Nice", "France", new DateTime(2024,05,25), 43.7102, 7.262),
            ("Monaco", "Monaco", new DateTime(2024,05,26), 43.7384, 7.4246),
            ("Nice", "France", new DateTime(2024,05,26), 43.7102, 7.262),
            ("Barcelona", "Spain", new DateTime(2024,05,28), 41.3851, 2.1734),
            ("Prague", "Czech Republic", new DateTime(2024,05,28), 50.0755, 14.4378),
            ("Karlovy Vary", "Czech Republic", new DateTime(2024,07,06), 50.231, 12.8712),
            ("Istanbul", "Turkey", new DateTime(2024,07,17), 41.0082, 28.9784),
            ("Almaty", "Kazakhstan", new DateTime(2024,07,18), 43.2389, 76.8897),
            ("Dubai", "UAE", new DateTime(2024,07,31), 25.276987, 55.296249),
            ("Almaty", "Kazakhstan", new DateTime(2024,08,06), 43.2389, 76.8897),
            ("Istanbul", "Turkey", new DateTime(2024,08,26), 41.0082, 28.9784),
            ("Prague", "Czech Republic", new DateTime(2024,08,26), 50.0755, 14.4378),
            ("Batumi", "Georgia", new DateTime(2024,09,26), 41.6168, 41.6367),
            ("Berlin", "Germany", new DateTime(2024,09,30), 52.52, 13.405),
            ("Prague", "Czech Republic", new DateTime(2024,09,30), 50.0755, 14.4378),
            ("Milan", "Italy", new DateTime(2024,11,09), 45.4642, 9.19),
            ("Prague", "Czech Republic", new DateTime(2024,11,10), 50.0755, 14.4378),
            ("Rome", "Italy", new DateTime(2024,12,21), 41.9028, 12.4964),
            ("Vatican City", "Vatican", new DateTime(2024,12,21), 41.9028, 12.4964),
            ("Abu Dhabi", "UAE", new DateTime(2024,12,22), 24.4539, 54.3773),
            ("Almaty", "Kazakhstan", new DateTime(2024,12,22), 43.2389, 76.8897),
            ("Doha", "Qatar", new DateTime(2025,01,05), 25.276987, 51.520008),
            ("Prague", "Czech Republic", new DateTime(2025,01,06), 50.0755, 14.4378),
            ("Karlštejn", "Czech Republic", new DateTime(2025,01,24), 49.9379, 14.1898),
            ("Prague", "Czech Republic", new DateTime(2025,01,24), 50.0755, 14.4378),
            ("Tirana", "Albania", new DateTime(2025,01,31), 41.3275, 19.8189),
            ("Pristina", "Kosovo", new DateTime(2025,02,01), 42.6629, 21.1655),
            ("Skopje", "North Macedonia", new DateTime(2025,02,02), 41.9981, 21.4254),
            ("Malta", "Malta", new DateTime(2025,02,03), 35.9375, 14.3754),
            ("Katowice", "Poland", new DateTime(2025,02,05), 50.2649, 19.0238),
            ("Prague", "Czech Republic", new DateTime(2025,02,06), 50.0755, 14.4378),
            ("Budapest", "Hungary", new DateTime(2025,02,15), 47.4979, 19.0402),
            ("Prague", "Czech Republic", new DateTime(2025,02,16), 50.0755, 14.4378),
            ("Milan", "Italy", new DateTime(2025,04,24), 45.4642, 9.19),
            ("Bologna", "Italy", new DateTime(2025,04,24), 44.4949, 11.3426),
            ("Rome", "Italy", new DateTime(2025,04,25), 41.9028, 12.4964),
            ("Vatican City", "Vatican", new DateTime(2025,04,25), 41.9028, 12.4964),
            ("Budapest", "Hungary", new DateTime(2025,04,27), 47.4979, 19.0402),
            ("Prague", "Czech Republic", new DateTime(2025,04,28), 50.0755, 14.4378),
            ("Tabor", "Czech Republic", new DateTime(2025,06,02), 49.4144, 14.6578),
            ("Prague", "Czech Republic", new DateTime(2025,06,02), 50.0755, 14.4378),
            ("Budapest", "Hungary", new DateTime(2025,06,24), 47.4979, 19.0402),
            ("Yerevan", "Armenia", new DateTime(2025,06,26), 40.1792, 44.4991),
            ("Larnaca", "Cyprus", new DateTime(2025,06,27), 34.9, 33.6333),
            ("Nicosia", "Northern Cyprus", new DateTime(2025,06,27), 35.1856, 33.3823),
            ("Cairo", "Egypt", new DateTime(2025,06,28), 30.0444, 31.2357),
            ("Rome", "Italy", new DateTime(2025,06,30), 41.9028, 12.4964),
            ("Prague", "Czech Republic", new DateTime(2025,06,30), 50.0755, 14.4378),
            ("Warsaw", "Poland", new DateTime(2025,07,03), 52.2297, 21.0122),
            ("Lisbon", "Portugal", new DateTime(2025,07,05), 38.7169, -9.1399),
            ("Malaga", "Spain", new DateTime(2025,07,08), 36.7213, -4.4214),
            ("Tenerife", "Spain", new DateTime(2025,07,08), 28.2916, -16.6291),
            ("Budapest", "Hungary", new DateTime(2025,07,14), 47.4979, 19.0402),
            ("Larnaca", "Cyprus", new DateTime(2025,07,14), 34.9, 33.6333),
            ("Abu Dhabi", "UAE", new DateTime(2025,07,15), 24.4539, 54.3773),
            ("Almaty", "Kazakhstan", new DateTime(2025,07,15), 43.2389, 76.8897),
            ("Abu Dhabi", "UAE", new DateTime(2025,07,24), 24.4539, 54.3773),
            ("Larnaca", "Cyprus", new DateTime(2025,07,24), 34.9, 33.6333),
            ("Vienna", "Austria", new DateTime(2025,07,25), 48.2082, 16.3738),
            ("Prague", "Czech Republic", new DateTime(2025,07,25), 50.0755, 14.4378),
            ("Istanbul", "Turkey", new DateTime(2025,08,27), 41.0082, 28.9784),
            ("Seoul", "South Korea", new DateTime(2025,08,28), 37.5665, 126.978),
            ("Beijing", "China", new DateTime(2025,09,08), 39.9042, 116.4074),
            ("Shanghai", "China", new DateTime(2025,09,12), 31.2304, 121.4737),
            ("Seoul", "South Korea", new DateTime(2025,09,14), 37.5665, 126.978),
            ("Ho Chi Minh City", "Vietnam", new DateTime(2025,10,07), 10.7769, 106.7009),
            ("Bangkok", "Thailand", new DateTime(2025,10,10), 13.7563, 100.5018),
            ("Bandar Seri Begawan", "Brunei", new DateTime(2025,10,13), 4.9031, 114.9398),
            ("Seoul", "South Korea", new DateTime(2025,10,14), 37.5665, 126.978)
        };

        for (int i = 0; i < tripsData.Length; i++)
        {
            var (cityName, countryName, startDate, lat, lon) = tripsData[i];

            var country = db.Countries.FirstOrDefault(c => c.Name == countryName);
            if (country == null)
            {
                country = new Country { Name = countryName, Code = countryName.Substring(0, 2).ToUpper() };
                db.Countries.Add(country);
                db.SaveChanges();
            }

            var city = db.Cities.FirstOrDefault(c => c.Name == cityName);
            if (city == null)
            {
                city = new City { Name = cityName, Latitude = lat, Longitude = lon, Country = country };
                db.Cities.Add(city);
                db.SaveChanges();
            }

            var nextDate = (i + 1 < tripsData.Length) ? tripsData[i + 1].StartDate : startDate.AddDays(2);

            if (!db.Trips.Any(t => t.City.Name == cityName && t.StartDate == startDate))
            {
                db.Trips.Add(new Trip
                {
                    Title = $"Trip to {cityName}",
                    Description = $"Memorable journey in {cityName}, {countryName}.",
                    StartDate = startDate,
                    EndDate = nextDate,
                    City = city
                });
            }
        }

        await db.SaveChangesAsync();
    }

    // ===== SEED ADMIN (из конфигурации) =====
    var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = sp.GetRequiredService<UserManager<IdentityUser>>();
    const string adminRole = "Admin";

    if (!await roleMgr.RoleExistsAsync(adminRole))
        await roleMgr.CreateAsync(new IdentityRole(adminRole));

    var adminEmail = builder.Configuration["Admin:Email"];
    var adminPass  = builder.Configuration["Admin:Password"];
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPass))
        throw new InvalidOperationException("Set Admin:Email and Admin:Password in configuration (User Secrets or appsettings).");

    var user = await userMgr.FindByEmailAsync(adminEmail);
    if (user == null)
    {
        user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var res = await userMgr.CreateAsync(user, adminPass);
        if (!res.Succeeded)
            throw new Exception("Failed to create admin: " + string.Join("; ", res.Errors.Select(e => $"{e.Code}:{e.Description}")));
    }
    if (!await userMgr.IsInRoleAsync(user, adminRole))
        await userMgr.AddToRoleAsync(user, adminRole);
}

app.Run();
