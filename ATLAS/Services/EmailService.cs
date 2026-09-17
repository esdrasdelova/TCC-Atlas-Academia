using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Envio de e-mails com dois provedores suportados:
///  1. Resend (API REST em HTTPS — chave permanente, nenhuma porta de SMTP).
///  2. SMTP via System.Net.Mail (Gmail/Brevo) como fallback, sem dependências externas.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public Task<bool> EnviarAsync(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.Remetente) || mensagem.Para.Count == 0)
        {
            _logger.LogWarning("E-mail não enviado: remetente ou destinatário ausente.");
            return Task.FromResult(false);
        }

        // Resend (API) tem prioridade quando configurado — é o caminho permanente.
        return !string.IsNullOrWhiteSpace(_config.Resend?.ApiKey)
            ? EnviarViaResendAsync(mensagem)
            : EnviarViaSmtpAsync(mensagem);
    }

    private async Task<bool> EnviarViaSmtpAsync(EmailMensagem mensagem)
    {
        if (string.IsNullOrWhiteSpace(_config.Senha))
        {
            _logger.LogWarning("E-mail não enviado: senha SMTP não configurada (Email__Senha).");
            return false;
        }

        try
        {
#pragma warning disable SYSLIB0014
            using var cliente = new SmtpClient(_config.Smtp.Host, _config.Smtp.Porta)
            {
                EnableSsl = _config.Smtp.UsarSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    string.IsNullOrWhiteSpace(_config.Smtp.Usuario) ? _config.Remetente : _config.Smtp.Usuario,
                    _config.Senha),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_config.Remetente, _config.NomeExibicao),
                Subject = mensagem.Assunto,
                Body = mensagem.Corpo,
                IsBodyHtml = mensagem.Html
            };
            mail.To.Add(mensagem.Para[0]);
            for (int i = 1; i < mensagem.Para.Count; i++)
                mail.To.Add(mensagem.Para[i]);

            await cliente.SendMailAsync(mail);
#pragma warning restore SYSLIB0014

            _logger.LogInformation("E-mail enviado para {Para} (assunto: {Assunto})", string.Join(", ", mensagem.Para), mensagem.Assunto);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Para}", mensagem.Para);
            return false;
        }
    }

    /// <summary>
    /// Envio via API REST do Resend (https://api.resend.com/emails). HTTPS puro,
    /// sem senha rotativa — a chave re_ é permanente e funciona de qualquer máquina.
    /// </summary>
    private async Task<bool> EnviarViaResendAsync(EmailMensagem mensagem)
    {
        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["from"] = $"{_config.NomeExibicao} <{_config.Remetente}>",
                ["to"] = mensagem.Para,
                ["subject"] = mensagem.Assunto,
                [mensagem.Html ? "html" : "text"] = mensagem.Corpo
            };

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.Resend!.ApiKey);
            using var resposta = await http.PostAsync(
                "https://api.resend.com/emails",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var corpoResposta = await resposta.Content.ReadAsStringAsync();
            if (!resposta.IsSuccessStatusCode)
            {
                _logger.LogError("Resend falhou ({Status}): {Corpo} para {Para}",
                    (int)resposta.StatusCode, corpoResposta, string.Join(", ", mensagem.Para));
                return false;
            }

            _logger.LogInformation("E-mail enviado via Resend para {Para} (assunto: {Assunto})", string.Join(", ", mensagem.Para), mensagem.Assunto);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar via Resend para {Para}", mensagem.Para);
            return false;
        }
    }

    public async Task<bool> EnviarCodigoRecuperacaoAsync(string emailDestino, string codigo, string nomeUsuario)
    {
        var primeiroNome = (nomeUsuario ?? string.Empty).Split(' ').FirstOrDefault() ?? string.Empty;
        var corpo = string.Concat(
            "<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\" /><title>Recuperação de senha</title></head>",
            "<body style=\"margin:0;padding:0;background-color:#101013;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#f4f4f5;\">",
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" style=\"background-color:#101013;padding:32px 16px;\"><tr><td align=\"center\">",
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"100%\" style=\"max-width:560px;background-color:#18181b;border-radius:12px;padding:32px;\">",
            "<tr><td style=\"font-size:22px;font-weight:600;color:#ffffff;\">Atlas Centro de Treinamento</td></tr>",
            "<tr><td style=\"padding-top:8px;font-size:15px;color:#a1a1aa;\">Olá, ", System.Net.WebUtility.HtmlEncode(primeiroNome), ".</td></tr>",
            "<tr><td style=\"padding-top:16px;font-size:15px;color:#e4e4e7;line-height:1.5;\">Recebemos uma solicitação para redefinir a senha da sua conta. Use o código abaixo para criar uma nova senha. O código expira em <strong style=\"color:#b8ff3d;\">15 minutos</strong>.</td></tr>",
            "<tr><td align=\"center\" style=\"padding:28px 0;\">",
            "<div style=\"display:inline-block;background-color:#101013;border:2px solid #b8ff3d;border-radius:8px;padding:18px 28px;\">",
            "<span style=\"font-family:'Courier New',Consolas,monospace;font-size:32px;font-weight:700;letter-spacing:8px;color:#b8ff3d;\">", System.Net.WebUtility.HtmlEncode(codigo), "</span>",
            "</div></td></tr>",
            "<tr><td style=\"font-size:13px;color:#a1a1aa;line-height:1.5;\">Se você não solicitou a recuperação, ignore esta mensagem. Alguém pode ter digitado o seu e-mail por engano.</td></tr>",
            "<tr><td style=\"padding-top:24px;border-top:1px solid #27272a;margin-top:24px;font-size:12px;color:#71717a;\">Em caso de dúvidas, fale com a gente em <a href=\"mailto:contato@atlasct.com.br\" style=\"color:#b8ff3d;text-decoration:none;\">contato@atlasct.com.br</a>.</td></tr>",
            "</table></td></tr></table></body></html>"
        );

        var mensagem = new EmailMensagem
        {
            Assunto = "Atlas - Código de recuperação de senha",
            Corpo = corpo,
            Html = true
        };
        mensagem.Para.Add(emailDestino);

        return await EnviarAsync(mensagem);
    }
}