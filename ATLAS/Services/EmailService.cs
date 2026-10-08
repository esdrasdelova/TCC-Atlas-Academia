using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Envio de e-mails: mock (desenvolvimento) ou real via Resend / Brevo / SMTP.
/// Sem pacote externo — usa HttpClient (APIs HTTP) e SmtpClient (SMTP genérico/Gmail).
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _contentRootPath;
    private readonly IHttpClientFactory _httpFactory;

    public EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger, IWebHostEnvironment env, IHttpClientFactory httpFactory)
    {
        _config = config.Value;
        _logger = logger;
        _contentRootPath = env.ContentRootPath;
        _httpFactory = httpFactory;
    }

    public bool Configurado
    {
        get
        {
            var modo = (_config.Modo ?? "mock").Trim().ToLowerInvariant();
            if (modo == "mock") return true;
            if (modo is "resend" or "brevo") return !string.IsNullOrWhiteSpace(_config.ApiKey);
            if (modo == "smtp") return !string.IsNullOrWhiteSpace(_config.Smtp?.Senha) && !string.IsNullOrWhiteSpace(_config.Smtp?.Usuario);
            return false;
        }
    }

    public async Task<bool> EnviarAsync(EmailMensagem mensagem) =>
        await EnviarComStatusAsync(mensagem) == StatusEnvio.Enviado;

    private async Task<StatusEnvio> EnviarComStatusAsync(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.Remetente) || mensagem.Para.Count == 0)
        {
            _logger.LogError("E-mail não enviado: remetente ou destinatário ausente.");
            return StatusEnvio.NaoConfigurado;
        }

        var modo = (_config.Modo ?? "mock").Trim().ToLowerInvariant();
        try
        {
            return modo switch
            {
                "resend" => await EnviarViaResendAsync(mensagem),
                "brevo" => await EnviarViaBrevoAsync(mensagem),
                "smtp" => await EnviarViaSmtpAsync(mensagem),
                _ => SalvarEmailMock(mensagem),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail via '{Modo}'.", modo);
            return StatusEnvio.Falhou;
        }
    }

    // ---- Resend: POST https://api.resend.com/emails (Authorization: Bearer re_...) ----
    private async Task<StatusEnvio> EnviarViaResendAsync(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _logger.LogError("Resend não configurado: EMAIL__ApiKey vazio.");
            return StatusEnvio.NaoConfigurado;
        }

        var client = _httpFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey.Trim());

        var from = string.IsNullOrWhiteSpace(_config.NomeExibicao)
            ? _config.Remetente
            : $"{_config.NomeExibicao} <{_config.Remetente}>";

        var payload = new
        {
            from,
            to = mensagem.Para.ToArray(),
            subject = mensagem.Assunto,
            html = mensagem.Html ? mensagem.Corpo : null,
            text = mensagem.Html ? (object?)null : mensagem.Corpo
        };

        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await client.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Resend recusou o envio ({Status}): {Body}", (int)resp.StatusCode, body);
            return StatusEnvio.Falhou;
        }

        _logger.LogInformation("E-mail enviado via Resend para {Para}.", string.Join(", ", mensagem.Para));
        return StatusEnvio.Enviado;
    }

    // ---- Brevo: POST https://api.brevo.com/v3/smtp/email (header api-key) ----
    // Aceita a mesma chave SMTP/API (xkeysib-...). Remetente precisa estar verificado em Senders.
    private async Task<StatusEnvio> EnviarViaBrevoAsync(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _logger.LogError("Brevo não configurado: EMAIL__ApiKey vazio (gere em SMTP & API).");
            return StatusEnvio.NaoConfigurado;
        }

        var client = _httpFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        req.Headers.Add("api-key", _config.ApiKey.Trim());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            sender = new { name = _config.NomeExibicao, email = _config.Remetente },
            to = mensagem.Para.Select(e => new { email = e }).ToArray(),
            subject = mensagem.Assunto,
            htmlContent = mensagem.Html ? mensagem.Corpo : null,
            textContent = mensagem.Html ? (object?)null : mensagem.Corpo
        };

        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await client.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Brevo recusou o envio ({Status}): {Body}", (int)resp.StatusCode, body);
            return StatusEnvio.Falhou;
        }

        _logger.LogInformation("E-mail enviado via Brevo para {Para}.", string.Join(", ", mensagem.Para));
        return StatusEnvio.Enviado;
    }

    // ---- SMTP genérico (Gmail, Brevo relay, etc.) ----
    private async Task<StatusEnvio> EnviarViaSmtpAsync(EmailMensagem mensagem)
    {
        var smtp = _config.Smtp;
        if (smtp is null || string.IsNullOrWhiteSpace(smtp.Usuario) || string.IsNullOrWhiteSpace(smtp.Senha))
        {
            _logger.LogError("SMTP não configurado: EMAIL__Smtp__Usuario/Senha vazios.");
            return StatusEnvio.NaoConfigurado;
        }

        using var client = new SmtpClient(smtp.Host, smtp.Porta)
        {
            EnableSsl = smtp.UsarSsl,
            Credentials = new NetworkCredential(smtp.Usuario, smtp.Senha),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_config.Remetente, _config.NomeExibicao),
            Subject = mensagem.Assunto,
            Body = mensagem.Corpo,
            IsBodyHtml = mensagem.Html
        };
        foreach (var para in mensagem.Para)
            mail.To.Add(para);

        await client.SendMailAsync(mail);
        _logger.LogInformation("E-mail enviado via SMTP ({Host}) para {Para}.", smtp.Host, string.Join(", ", mensagem.Para));
        return StatusEnvio.Enviado;
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
        EnviarLinkRecuperacaoComStatusAsync(emailDestino, linkRedefinicao, nomeUsuario)
            .ContinueWith(t => t.Result == StatusEnvio.Enviado, TaskScheduler.Default);

    public async Task<StatusEnvio> EnviarLinkRecuperacaoComStatusAsync(string emailDestino, string linkRedefinicao, string nomeUsuario)
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

        return await EnviarComStatusAsync(mensagem);
    }
}
