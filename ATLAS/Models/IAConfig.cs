namespace ATLAS.Models;

/// <summary>
/// Configuração do assistente virtual (mock/local).
/// Não usa API externa — respostas são pré-definidas no serviço.
/// </summary>
public class IAConfig
{
    /// <summary>Habilita/desabilita o assistente (padrão: true).</summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>Nome do assistente exibido no chat.</summary>
    public string Nome { get; set; } = "Assistente Atlas";
}
