using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Controllers;

[Route("sobre")]
public class SobreController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Sobre nós";
        return View();
    }
}
