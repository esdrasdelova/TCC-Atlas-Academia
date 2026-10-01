namespace ATLAS.Models.Entities;

/// <summary>
/// Banco de exercícios da academia — o personal seleciona exercícios
/// existentes ao montar um treino, sem digitar tudo manualmente.
/// </summary>
public class Exercicio
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    /// <summary>Ex.: Peitoral, Costas, Pernas…</summary>
    public string GrupoMuscular { get; set; } = string.Empty;

    public string? ImagemUrl { get; set; }

    public string? VideoUrl { get; set; }

    public string? Instrucoes { get; set; }

    public bool Ativo { get; set; } = true;
}
