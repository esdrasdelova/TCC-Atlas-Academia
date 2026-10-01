namespace ATLAS.Models.Entities;

public class Plano
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public decimal PrecoMensal { get; set; }

    /// <summary>Benefícios exibidos no site, um por linha.</summary>
    public string? Beneficios { get; set; }

    public int OrdemExibicao { get; set; }

    public bool Ativo { get; set; } = true;
}
