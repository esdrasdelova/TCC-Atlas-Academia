namespace ATLAS.Models.Entities;

/// <summary>
/// Foto de progresso enviada pelo aluno. A imagem fica em
/// wwwroot/uploads/progresso e o banco guarda apenas o caminho relativo.
/// </summary>
public class FotoProgresso
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    /// <summary>Caminho relativo da imagem (ex.: /uploads/progresso/3_20260101.jpg).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Legenda/ângulo da foto (ex.: "Frente", "Lado", "Costas").</summary>
    public string? Legenda { get; set; }

    public DateTime EnviadaEm { get; set; } = DateTime.UtcNow;
}
