namespace ATLAS.Models;

/// <summary>
/// Configuração do envio de e-mails via SMTP (seção "Email" do appsettings.json).
/// Para Gmail, o Remetente precisa usar uma "Senha de app" (com verificação em 2
/// etapas ativada) — a senha comum da conta não funciona no SMTP do Gmail.
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