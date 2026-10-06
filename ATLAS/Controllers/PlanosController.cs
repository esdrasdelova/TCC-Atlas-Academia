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
                Nome = "Aula Avulsa",
                Preco = "20",
                Periodo = "por aula",
                Descricao = "Conheça a Atlas sem compromisso, no seu ritmo.",
                TextoBotao = "Agendar aula",
                Icone = "raio",
                Beneficios = new List<string>
                {
                    "1 aula de musculação ou funcional",
                    "Orientação de instrutor",
                    "Sem matrícula e sem fidelidade",
                    "Ideal para experimentar"
                }
            },
            new PlanoItem
            {
                Nome = "Mensal",
                Preco = "100",
                Periodo = "por mês",
                Descricao = "Liberdade total para treinar no seu ritmo.",
                Icone = "calendario",
                Beneficios = new List<string>
                {
                    "Acesso livre à musculação",
                    "App de treinos Atlas",
                    "Avaliação física inicial",
                    "Sem taxa de matrícula",
                    "Cancele quando quiser"
                }
            },
            new PlanoItem
            {
                Nome = "Trimestral",
                Preco = "260",
                Periodo = "a cada 3 meses",
                Descricao = "O equilíbrio perfeito entre resultado e economia.",
                Destaque = true,
                Selo = "Mais escolhido",
                PrecoInfo = "Equivale a R$ 86,67/mês · economize R$ 40",
                Icone = "trofeu",
                Beneficios = new List<string>
                {
                    "Tudo do plano Mensal",
                    "Treinamento funcional liberado",
                    "Acompanhamento de personal",
                    "Reavaliação física mensal",
                    "Prioridade nos agendamentos"
                }
            }
        };

        return View(planos);
    }
}
