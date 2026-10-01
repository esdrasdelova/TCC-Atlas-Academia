using ATLAS.Models;
using ATLAS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ATLAS.Controllers;

[Route("planos")]
public class PlanosController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Planos";

        var planos = new List<PlanoItem>
        {
            new PlanoItem
            {
                Nome = "Mensal",
                Preco = "R$ 119,90",
                Descricao = "Liberdade total, sem fidelidade.",
                Beneficios = new List<string>
                {
                    "Acesso livre à musculação",
                    "App de treinos Atlas",
                    "Avaliação física inicial"
                }
            },
            new PlanoItem
            {
                Nome = "Trimestral",
                Preco = "R$ 99,90",
                Descricao = "O equilíbrio perfeito. O mais escolhido.",
                Destaque = true,
                Beneficios = new List<string>
                {
                    "Tudo do plano Mensal",
                    "Treinamento funcional liberado",
                    "Acompanhamento de personal",
                    "Reavaliação física mensal"
                }
            },
            new PlanoItem
            {
                Nome = "Anual",
                Preco = "R$ 89,90",
                Descricao = "Para quem veio para a evolução completa.",
                Beneficios = new List<string>
                {
                    "Tudo do plano Trimestral",
                    "Pilates (2x por semana)",
                    "Avaliação física trimestral",
                    "Acesso prioritário a agendamentos"
                }
            }
        };

        return View(planos);
    }
}
