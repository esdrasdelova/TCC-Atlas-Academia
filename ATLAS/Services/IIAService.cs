namespace ATLAS.Services;

public enum StatusIA
{
    NaoConfigurado,
    Sucesso,
    Indisponivel,
    LimiteExcedido,
    Erro
}

public record RespostaIA(StatusIA Status, string Texto)
{
    public bool Sucesso => Status == StatusIA.Sucesso;
}

/// <summary>Mensagem do histórico enviada ao modelo (sem dados sensíveis).</summary>
public record MensagemIA(string Papel, string Conteudo);

public interface IIAService
{
    bool Configurado { get; }

    Task<RespostaIA> ConversarAsync(IReadOnlyList<MensagemIA> historico, string mensagem, CancellationToken ct = default);
}
