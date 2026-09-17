namespace ATLAS.Models;

/// <summary>
/// Configuração do envio de e-mails (seção "Email" do appsettings.json e
/// variáveis de ambiente com prefixo Email__, ex.: Email__Senha).
///
/// Dois caminhos suportados, na ordem de preferência:
///  1. Resend (API HTTPS — credencial permanente, funciona de qualquer máquina):
///     basta Email__Resend__ApiKey. Sem SMTP, sem senha de app.
///  2. SMTP (System.Net.Mail) — Gmail, Brevo, etc. Para Gmail o Remetente precisa
///     de "senha de app" (verificação em 2 etapas), que o Google rotaciona — por
///     isso em projetos reais prefira Resend ou Brevo (chaves permanentes).
/// </summary>
public class EmailConfig
{
    public string Remetente { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string NomeExibicao { get; set; } = "Atlas Centro de Treinamento";

    /// <summary>
    /// Caixa que recebe as mensagens do site. Se vazio, usa o e-mail da seção SiteConfig.
    /// </summary>
    public string Destino { get; set; } = string.Empty;

    public SmtpConfig Smtp { get; set; } = new();

    /// <summary>Caminho preferido de envio quando a chave está preenchida.</summary>
    public ResendConfig Resend { get; set; } = new();
}

public class ResendConfig
{
    /// <summary>Chave de API permanente (ex.: re_xxxxxxxx). Define a env Email__Resend__ApiKey.</summary>
    public string ApiKey { get; set; } = string.Empty;
}

public class SmtpConfig
{
    public string Host { get; set; } = "smtp-relay.brevo.com";
    public int Porta { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;

    /// <summary>
    /// Usuário do login SMTP. Se vazio, o serviço usa o Remetente como usuário
    /// (comportamento do Gmail). No Brevo, o login é o e-mail da conta Brevo.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;
}