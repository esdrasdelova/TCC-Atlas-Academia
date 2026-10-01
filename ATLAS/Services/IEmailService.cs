namespace ATLAS.Services;

public class EmailMensagem
{
    /// <summary>Um ou mais destinatários. Cada e-mail pode receber uma cópia.</summary>
    public List<string> Para { get; set; } = new();
    public string Assunto { get; set; } = string.Empty;
    public string Corpo { get; set; } = string.Empty;
    public bool Html { get; set; }
}

/// <summary>
/// Resultado detalhado de um envio. Permite tratar cada causa de forma honesta
/// (sem fingir que o e-mail saiu) e distinguir "servidor sem credencial" de
/// "provedor recusou o envio".
/// </summary>
public enum StatusEnvio
{
    /// <summary>O provedor aceitou a mensagem para entrega.</summary>
    Enviado,

    /// <summary>Nenhuma credencial configurada (Email__Senha ou Email__Resend__ApiKey).</summary>
    NaoConfigurado,

    /// <summary>Credencial presente, mas o provedor falhou ao enviar.</summary>
    Falhou
}

public interface IEmailService
{
    /// <summary>
    /// Indica se existe credencial de envio (Resend ou SMTP) configurada.
    /// Quando false, nenhum e-mail sai do servidor — inclusive o de recuperação de senha.
    /// </summary>
    bool Configurado { get; }

    /// <summary>Envia um e-mail via SMTP/Resend. Retorna true se o envio foi concluído.</summary>
    Task<bool> EnviarAsync(EmailMensagem mensagem);

    /// <summary>
    /// Envia o link (token opaco) de redefinição de senha para o e-mail informado.
    /// Retorna true apenas se o e-mail foi realmente entregue ao provedor.
    /// </summary>
    Task<bool> EnviarLinkRecuperacaoAsync(string emailDestino, string linkRedefinicao, string nomeUsuario);

    /// <summary>
    /// Igual a <see cref="EnviarLinkRecuperacaoAsync"/>, mas informa o motivo real
    /// em caso de falha (<see cref="StatusEnvio.NaoConfigurado"/> ou <see cref="StatusEnvio.Falhou"/>).
    /// </summary>
    Task<StatusEnvio> EnviarLinkRecuperacaoComStatusAsync(string emailDestino, string linkRedefinicao, string nomeUsuario);
}