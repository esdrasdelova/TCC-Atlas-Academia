using ATLAS.Models;
using ATLAS.Models.ViewModels;
using ATLAS.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ATLAS.Controllers;

public class HomeController : Controller
{
    private readonly ISiteConfigService _siteConfig;

    public HomeController(ISiteConfigService siteConfig)
    {
        _siteConfig = siteConfig;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Início";
        var config = _siteConfig.Obter();

        var viewModel = new HomeViewModel
        {
            Destaques = config.Destaques,
            Modalidades = config.Modalidades
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacidade";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Title"] = "Erro";
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
