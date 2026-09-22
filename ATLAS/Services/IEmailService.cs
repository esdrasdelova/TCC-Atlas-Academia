namespace ATLAS.Services;

public class EmailMensagem
{
    /// <summary>Um ou mais destinatários. Cada e-mail pode receber uma cópia.</summary>
    public List<string> Para { get; set; } = new();
    public string Assunto { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public bool Html { get; set; }
}

public interface IEmailService
{
    /// <summary>Envia um e-mail via SMTP. Retorna true se o envio foi concluído.</summary>
    Task<bool> EnviarAsync(EmailMensagem mensagem);

    /// <summary>
    /// Envia o link (token opaco) de redefinição de senha para o e-mail informado.
    /// Retorna true apenas se o e-mail foi realmente entregue ao provedor.
    /// </summary>
    Task<bool> EnviarLinkRecuperacaoAsync(string emailDestino, string linkRedefinicao, string nomeUsuario);
}