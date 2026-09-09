namespace ATLAS.Models.ViewModels;

/// <summary>
/// Plano exibido na página de Planos. Futuramente virá da tabela Planos do banco,
/// editável pelo painel administrativo.
/// </summary>
public class PlanoItem
{
    public string Nome { get; set; } = string.Empty;
    public string Preco { get; set; } = string.Empty;
    public string Periodo { get; set; } = "por mês";
    public string? Descricao { get; set; }
    public List<string> Beneficios { get; set; } = new();
    public bool Destaque { get; set; }
}