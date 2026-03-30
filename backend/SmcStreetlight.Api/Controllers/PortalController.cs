using Microsoft.AspNetCore.Mvc;

namespace SmcStreetlight.Api.Controllers;

public class PortalController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Track() => RedirectToAction(nameof(Index));
    public IActionResult Login() => RedirectToAction(nameof(Index));
    public IActionResult Dashboard() => RedirectToAction(nameof(Index));
}
