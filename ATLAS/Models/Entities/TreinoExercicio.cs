namespace ATLAS.Models.Entities;

/// <summary>
/// Item de um treino: liga o treino a um exercício do banco de exercícios,
/// carregando as prescrições (séries, repetições, descanso e ordem de execução).
/// </summary>
public class TreinoExercicio
{
    public int Id { get; set; }

    public int TreinoId { get; set; }
    public Treino Treino { get; set; } = null!;

    public int ExercicioId { get; set; }
    public Exercicio Exercicio { get; set; } = null!;

    /// <summary>Posição do exercício dentro do treino (1º, 2º, 3º…).</summary>
    public int Ordem { get; set; }

    public int Series { get; set; }

    /// <summary>Ex.: 10 — ou texto livre como "10-12" ou "até a falha".</summary>
    public string Repeticoes { get; set; } = string.Empty;

    /// <summary>Carga prescrita, ex.: "20 kg" — texto livre para caber máquinas e halteres.</summary>
    public string? Carga { get; set; }

    /// <summary>Tempo de descanso entre séries, em segundos. Ex.: 60.</summary>
    public int DescansoSegundos { get; set; }

    public string? Observacoes { get; set; }
}
