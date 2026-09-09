using ATLAS.Models;
using ATLAS.Services;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Controllers;

[Route("modalidades")]
public class ModalidadesController : Controller
{
    private readonly ISiteConfigService _siteConfig;

    public ModalidadesController(ISiteConfigService siteConfig)
    {
        _siteConfig = siteConfig;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Modalidades";
        return View(_siteConfig.Obter().Modalidades);
    }
}
