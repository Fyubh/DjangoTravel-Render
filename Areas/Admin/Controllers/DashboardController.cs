using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jango_Travel.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]                     // ← пускаем вообще всех (временно!)
public class DashboardController : Controller
{
    public IActionResult Index() => View();
}