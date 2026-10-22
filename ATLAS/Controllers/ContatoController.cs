using System.Net;
using ATLAS.Models;
using ATLAS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ATLAS.Controllers;

[Route("contato")]
public class ContatoController : Controller
{
    private readonly ISiteConfigService _siteConfig;
    private readonly IEmailService _email;
    private readonly EmailConfig _emailConfig;

    public ContatoController(ISiteConfigService siteConfig, IEmailService email, IOptions<EmailConfig> emailOptions)
    {
        _siteConfig = siteConfig;
        _email = email;
        _emailConfig = emailOptions.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Contato";
        return View(_siteConfig.Obter());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(string nome, string email, string assunto, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(assunto) || string.IsNullOrWhiteSpace(mensagem))
        {
            TempData["Erro"] = "Preencha todos os campos para enviar a mensagem.";
            return RedirectToAction(nameof(Index));
        }

        var destino = string.IsNullOrWhiteSpace(_emailConfig.Destino)
            ? _siteConfig.Obter().Email
            : _emailConfig.Destino;

        var corpo = $"""
            <div style="font-family:Arial, Helvetica, sans-serif; color:#222; line-height:1.6;">
                <h2 style="color:#111; margin:0 0 1rem;">Nova mensagem pelo site</h2>
                <p style="margin:0.3rem 0;"><strong>Nome:</strong> {WebUtility.HtmlEncode(nome)}</p>
                <p style="margin:0.3rem 0;"><strong>E-mail:</strong> {WebUtility.HtmlEncode(email)}</p>
                <p style="margin:0.3rem 0;"><strong>Assunto:</strong> {WebUtility.HtmlEncode(assunto)}</p>
                <p style="margin:0.3rem 0 0.6rem;"><strong>Mensagem:</strong></p>
                <blockquote style="margin:0 0 1rem; padding:0.8rem 1rem; background:#f5f5f5; border-left:4px solid #b8ff3d;">
                    {WebUtility.HtmlEncode(mensagem).Replace("\n", "<br>")}
                </blockquote>
                <p style="color:#777; font-size:12px;">Enviado pelo formulário de contato em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}.</p>
            </div>
            """;

        var enviado = await _email.EnviarAsync(new EmailMensagem
        {
            Para = MontarDestinos(destino, email),
            Assunto = $"[Contato - Site] {assunto}",
            Corpo = corpo,
            Html = true
        });

        TempData[enviado ? "Aviso" : "Erro"] = enviado
            ? "Mensagem enviada! Nossa equipe retornará em breve por e-mail."
            : "Não foi possível enviar sua mensagem agora. Tente novamente em instantes ou ligue para a academia.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Envia à academia (destino padrão) e também uma cópia ao e-mail informado pelo visitante.</summary>
    private static List<string> MontarDestinos(string destino, string visitante)
    {
        var destinatarios = new List<string>();
        if (!string.IsNullOrWhiteSpace(destino))
            destinatarios.Add(destino);
        if (!string.IsNullOrWhiteSpace(visitante) && !destinatarios.Contains(visitante.Trim()))
            destinatarios.Add(visitante.Trim());
        return destinatarios;
    }
}