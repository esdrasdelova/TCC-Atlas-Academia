namespace ATLAS.Models.Entities;

/// <summary>
/// Treino montado por um personal para um aluno específico (ex.: "Treino A — Peito e Tríceps").
/// Somente o aluno dono e o personal criador podem visualizá-lo.
/// </summary>
public class Treino
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public int PersonalId { get; set; }
    public PersonalTrainer Personal { get; set; } = null!;

    /// <summary>Nome curto, ex.: "Treino A".</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Foco do treino, ex.: "Peito e Tríceps" ou "Hipertrofia".</summary>
    public string? Objetivo { get; set; }

    public string? Observacoes { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    /// <summary>Treino publicado aparece na área do aluno.</summary>
    public bool Publicado { get; set; } = false;

    /// <summary>Marca o treino em uso pelo aluno no momento.</summary>
    public bool Ativo { get; set; } = true;

    public ICollection<TreinoExercicio> Itens { get; set; } = new List<TreinoExercicio>();
}
