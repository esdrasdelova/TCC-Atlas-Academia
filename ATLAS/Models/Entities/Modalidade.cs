namespace ATLAS.Models.Entities;

public class Modalidade
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    /// <summary>Identificador do ícone SVG usado no site (ex.: "dumbbell").</summary>
    public string Icone { get; set; } = "dumbbell";

    /// <summary>Ordem de exibição no site institucional.</summary>
    public int OrdemExibicao { get; set; }

    public bool Ativa { get; set; } = true;
}
