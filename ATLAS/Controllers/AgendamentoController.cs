using System.Net;
using ATLAS.Models;
using ATLAS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ATLAS.Controllers;

[Route("agendamento")]
public class AgendamentoController : Controller
{
    private readonly ISiteConfigService _siteConfig;
    private readonly IEmailService _email;
    private readonly EmailConfig _emailConfig;

    public AgendamentoController(ISiteConfigService siteConfig, IEmailService email, IOptions<EmailConfig> emailOptions)
    {
        _siteConfig = siteConfig;
        _email = email;
        _emailConfig = emailOptions.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Agende sua aula";
        return View(_siteConfig.Obter());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agendar(string nome, string telefone, string modalidade, DateTime data, string horario, string observacoes)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(telefone) ||
            string.IsNullOrWhiteSpace(modalidade) || data == default || string.IsNullOrWhiteSpace(horario))
        {
            TempData["Erro"] = "Preencha todos os campos obrigatórios do agendamento.";
            return RedirectToAction(nameof(Index));
        }

        var destino = string.IsNullOrWhiteSpace(_emailConfig.Destino)
            ? _siteConfig.Obter().Email
            : _emailConfig.Destino;

        var obs = string.IsNullOrWhiteSpace(observacoes) ? "—" : observacoes;

        var corpo = $"""
            <div style="font-family:Arial, Helvetica, sans-serif; color:#222; line-height:1.6;">
                <h2 style="color:#111; margin:0 0 1rem;">Solicitação de aula experimental</h2>
                <p style="margin:0.3rem 0;"><strong>Nome:</strong> {WebUtility.HtmlEncode(nome)}</p>
                <p style="margin:0.3rem 0;"><strong>WhatsApp:</strong> {WebUtility.HtmlEncode(telefone)}</p>
                <p style="margin:0.3rem 0;"><strong>Modalidade:</strong> {WebUtility.HtmlEncode(modalidade)}</p>
                <p style="margin:0.3rem 0;"><strong>Data preferida:</strong> {data:dd/MM/yyyy}</p>
                <p style="margin:0.3rem 0;"><strong>Horário:</strong> {WebUtility.HtmlEncode(horario)}</p>
                <p style="margin:0.3rem 0 0.6rem;"><strong>Observações:</strong></p>
                <blockquote style="margin:0 0 1rem; padding:0.8rem 1rem; background:#f5f5f5; border-left:4px solid #b8ff3d;">
                    {WebUtility.HtmlEncode(obs).Replace("\n", "<br>")}
                </blockquote>
                <p style="color:#777; font-size:12px;">Enviado pelo formulário de agendamento em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}.</p>
            </div>
            """;

        var enviado = await _email.EnviarAsync(new EmailMensagem
        {
            Para = new List<string> { destino },
            Assunto = $"[Agendamento - Site] Aula experimental de {modalidade}",
            Corpo = corpo,
            Html = true
        });

        TempData[enviado ? "Aviso" : "Erro"] = enviado
            ? $"Solicitação de aula de {modalidade} recebida! Nossa equipe confirmará pelo WhatsApp informado."
            : "Não foi possível registrar o agendamento agora. Tente novamente em instantes ou ligue para a academia.";

        return RedirectToAction(nameof(Index));
    }
}