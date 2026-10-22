namespace ATLAS.Models.ViewModels;

/// <summary>
/// Plano exibido na página de Planos. Futuramente virá da tabela Planos do banco,
/// editável pelo painel administrativo.
/// </summary>
public class PlanoItem
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Somente o número do preço (ex.: "260") — o "R$" é aplicado na view.</summary>
    public string Preco { get; set; } = string.Empty;

    public string Periodo { get; set; } = "por mês";
    public string? Descricao { get; set; }
    public List<string> Beneficios { get; set; } = new();
    public bool Destaque { get; set; }

    /// <summary>Selo exibido no topo do card (ex.: "Mais escolhido").</summary>
    public string? Selo { get; set; }

    /// <summary>Nota de economia/equivalência sob o preço (ex.: "equivale a R$ 86,70/mês").</summary>
    public string? PrecoInfo { get; set; }

    /// <summary>Texto do botão de ação do card.</summary>
    public string TextoBotao { get; set; } = "Começar agora";

    /// <summary>Ícone do card: "raio", "calendario" ou "trofeu".</summary>
    public string Icone { get; set; } = "calendario";
}
