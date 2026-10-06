namespace ATLAS.Models;

/// <summary>
/// Configuração do envio de e-mails (mock/local para desenvolvimento).
/// Em produção, trocar por implementação real (Resend, Brevo, SMTP, etc.).
/// </summary>
public class EmailConfig
{
    /// <summary>Modo de operação: "mock" (padrão, salva em log/arquivo), "resend", "brevo", "smtp".</summary>
    public string Modo { get; set; } = "mock";

    /// <summary>E-mail remetente (From).</summary>
    public string Remetente { get; set; } = "noreply@atlasct.com.br";

    /// <summary>Nome de exibição do remetente.</summary>
    public string NomeExibicao { get; set; } = "Atlas Centro de Treinamento";

    /// <summary>Caixa que recebe as mensagens do site (contato).</summary>
    public string Destino { get; set; } = "contato@atlasct.com.br";

    /// <summary>Pasta onde salvar emails simulados (apenas modo mock).</summary>
    public string PastaMock { get; set; } = "App_Data/Emails";

    // Configurações para provedores reais (futuro)
    public string ApiKey { get; set; } = string.Empty;
    public SmtpConfig Smtp { get; set; } = new();
}

public class SmtpConfig
{
    public string Host { get; set; } = "smtp-relay.brevo.com";
    public int Porta { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}