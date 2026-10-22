namespace ATLAS.Models.Entities;

/// <summary>
/// Registro de medidas corporais de um aluno, tirado em uma avaliação física.
/// Cada aluno pode ter várias medições ao longo do tempo, formando a evolução.
/// </summary>
public class MedidaCorporal
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    /// <summary>Data em que as medidas foram aferidas.</summary>
    public DateTime DataMedicao { get; set; } = DateTime.UtcNow;

    /// <summary>Peso em quilogramas.</summary>
    public decimal? PesoKg { get; set; }

    /// <summary>Altura em centímetros.</summary>
    public decimal? AlturaCm { get; set; }

    /// <summary>Medidas em centímetros — todas opcionais.</summary>
    public decimal? PeitoCm { get; set; }
    public decimal? CinturaCm { get; set; }
    public decimal? QuadrilCm { get; set; }
    public decimal? BracoCm { get; set; }
    public decimal? CoxaCm { get; set; }
    public decimal? PanturrilhaCm { get; set; }

    /// <summary>Percentual de gordura corporal (opcional).</summary>
    public decimal? PercentualGordura { get; set; }

    public string? Observacoes { get; set; }
}
