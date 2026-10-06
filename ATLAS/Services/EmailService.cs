using System.IO;
using System.Text;
using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Envio de e-mails MOCK (desenvolvimento) — salva em arquivo/log.
/// Para produção, implemente Resend/Brevo/SMTP trocando esta classe.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _contentRootPath;

    public EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger, IWebHostEnvironment env)
    {
        _config = config.Value;
        _logger = logger;
        _contentRootPath = env.ContentRootPath;
    }

    public bool Configurado => true; // Mock sempre "configurado"

    public Task<bool> EnviarAsync(EmailMensagem mensagem) =>
        Task.FromResult(EnviarComStatus(mensagem) == StatusEnvio.Enviado);

    private StatusEnvio EnviarComStatus(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.Remetente) || mensagem.Para.Count == 0)
        {
            _logger.LogError("E-mail não enviado: remetente ou destinatário ausente.");
            return StatusEnvio.NaoConfigurado;
        }

        if (_config.Modo.Equals("mock", StringComparison.OrdinalIgnoreCase))
        {
            return SalvarEmailMock(mensagem);
        }

        // Futuro: implementar Resend/Brevo/SMTP aqui
        _logger.LogWarning("Modo '{Modo}' não implementado — usando mock.", _config.Modo);
        return SalvarEmailMock(mensagem);
    }

    private StatusEnvio SalvarEmailMock(EmailMensagem mensagem)
    {
        try
        {
            var pasta = Path.Combine(_contentRootPath, _config.PastaMock);
            Directory.CreateDirectory(pasta);

            var arquivo = Path.Combine(pasta, $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.html");
            var html = GerarHtmlMock(mensagem);
            File.WriteAllText(arquivo, html, Encoding.UTF8);

            _logger.LogInformation("📧 [MOCK] E-mail salvo em: {Arquivo}", arquivo);
            _logger.LogInformation("📧 [MOCK] Para: {Para} | Assunto: {Assunto}", string.Join(", ", mensagem.Para), mensagem.Assunto);

            // Log do link de recuperação (para testar no browser)
            if (mensagem.Assunto.Contains("redefinir", StringComparison.OrdinalIgnoreCase))
            {
                var link = ExtrairLink(mensagem.Corpo);
                if (!string.IsNullOrEmpty(link))
                {
                    _logger.LogWarning("🔗 [MOCK] LINK DE RECUPERAÇÃO: {Link}", link);
                    Console.WriteLine($"\n=== LINK DE RECUPERAÇÃO DE SENHA ===\n{link}\n====================================\n");
                }
            }

            return StatusEnvio.Enviado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao salvar e-mail mock.");
            return StatusEnvio.Falhou;
        }
    }

    private string GerarHtmlMock(EmailMensagem mensagem)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Email Mock</title></head><body style='font-family:Arial,sans-serif;padding:20px;background:#f5f5f5;'>");
        sb.AppendLine("<div style='max-width:600px;margin:0 auto;background:white;padding:20px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);'>");
        sb.AppendLine($"<h2 style='color:#2c3e50;border-bottom:2px solid #3498db;padding-bottom:10px;'>📧 E-mail Simulado (Modo Desenvolvimento)</h2>");
        sb.AppendLine($"<p><strong>De:</strong> {_config.NomeExibicao} <{_config.Remetente}></p>");
        sb.AppendLine($"<p><strong>Para:</strong> {string.Join(", ", mensagem.Para)}</p>");
        sb.AppendLine($"<p><strong>Assunto:</strong> {mensagem.Assunto}</p>");
        sb.AppendLine("<hr style='border:1px solid #eee;'/>");
        sb.AppendLine(mensagem.Html ? mensagem.Corpo : $"<pre>{System.Web.HttpUtility.HtmlEncode(mensagem.Corpo)}</pre>");
        sb.AppendLine("<hr style='border:1px solid #eee;'/>");
        sb.AppendLine("<p style='color:#999;font-size:12px;'>Este e-mail NÃO foi enviado. É uma simulação para desenvolvimento.</p>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private string ExtrairLink(string html)
    {
        // Procura href="..." no HTML
        var idx = html.IndexOf("href=\"", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            idx += 6;
            var end = html.IndexOf("\"", idx, StringComparison.Ordinal);
            if (end > idx)
                return html.Substring(idx, end - idx);
        }
        return string.Empty;
    }

    public Task<bool> EnviarLinkRecuperacaoAsync(string emailDestino, string linkRedefinicao, string nomeUsuario) =>
        Task.FromResult(EnviarLinkRecuperacaoComStatusAsync(emailDestino, linkRedefinicao, nomeUsuario).Result == StatusEnvio.Enviado);

    public Task<StatusEnvio> EnviarLinkRecuperacaoComStatusAsync(string emailDestino, string linkRedefinicao, string nomeUsuario)
    {
        var primeiroNome = (nomeUsuario ?? string.Empty).Split(' ').FirstOrDefault() ?? string.Empty;
        var link = System.Net.WebUtility.HtmlEncode(linkRedefinicao);
        var corpo = string.Concat(
            "<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\" /><title>Recuperação de senha</title></head>",
            "<body style=\"margin:0;padding:0;background-color:#101013;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#f4f4f5;\">",
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" style=\"background-color:#101013;padding:32px 16px;\"><tr><td align=\"center\">",
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" style=\"max-width:560px;background-color:#18181b;border-radius:12px;padding:32px;\">",
            "<tr><td style=\"font-size:22px;font-weight:600;color:#ffffff;\">Atlas Centro de Treinamento</td></tr>",
            "<tr><td style=\"padding-top:8px;font-size:15px;color:#a1a1aa;\">Olá, ", System.Net.WebUtility.HtmlEncode(primeiroNome), ".</td></tr>",
            "<tr><td style=\"padding-top:16px;font-size:15px;color:#e4e4e7;line-height:1.5;\">Recebemos uma solicitação para redefinir a senha da sua conta. Clique no botão abaixo para escolher uma nova senha. O link expira em <strong style=\"color:#b8ff3d;\">30 minutos</strong> e só pode ser usado uma vez.</td></tr>",
            "<tr><td align=\"center\" style=\"padding:28px 0;\">",
            "<a href=\"", link, "\" style=\"display:inline-block;background-color:#b8ff3d;color:#101013;font-weight:600;text-decoration:none;border-radius:8px;padding:15px 30px;font-size:15px;\">Redefinir minha senha</a>",
            "</td></tr>",
            "<tr><td style=\"font-size:13px;color:#a1a1aa;line-height:1.5;\">Se o botão não funcionar, copie e cole este link no navegador:<br /><a href=\"", link, "\" style=\"color:#b8ff3d;word-break:break-all;\">", link, "</a></td></tr>",
            "<tr><td style=\"padding-top:16px;font-size:13px;color:#a1a1aa;line-height:1.5;\">Se você não solicitou a recuperação, ignore esta mensagem. Alguém pode ter digitado o seu e-mail por engano.</td></tr>",
            "<tr><td style=\"padding-top:24px;border-top:1px solid #27272a;margin-top:24px;font-size:12px;color:#71717a;\">Em caso de dúvidas, fale com a gente em <a href=\"mailto:contato@atlasct.com.br\" style=\"color:#b8ff3d;text-decoration:none;\">contato@atlasct.com.br</a>.</td></tr>",
            "</table></td></tr></table></body></html>"
        );

        var mensagem = new EmailMensagem
        {
            Assunto = "Atlas - Link para redefinir sua senha",
            Corpo = corpo,
            Html = true
        };
        mensagem.Para.Add(emailDestino);

        return Task.FromResult(EnviarComStatus(mensagem));
    }
}