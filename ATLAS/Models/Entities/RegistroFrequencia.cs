namespace ATLAS.Models.Entities;

/// <summary>
/// Registro de presença do aluno em um treino. Cada linha é um check-in,
/// usado para calcular a frequência no dashboard (resumo da semana e sequência).
/// </summary>
public class RegistroFrequencia
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    /// <summary>Data do treino (apenas o dia é considerado no cálculo).</summary>
    public DateTime DataTreino { get; set; } = DateTime.UtcNow;

    /// <summary>Nome do treino feito, quando disponível (ex.: "Treino A — Superiores").</summary>
    public string? TreinoNome { get; set; }
}
