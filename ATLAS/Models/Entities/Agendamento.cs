using ATLAS.Models.Enums;

namespace ATLAS.Models.Entities;

/// <summary>
/// Agendamentos de avaliações físicas, aulas experimentais e sessões com personal.
/// </summary>
public class Agendamento
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    public int? PersonalId { get; set; }
    public PersonalTrainer? Personal { get; set; }

    /// <summary>Ex.: "Avaliação física", "Aula experimental", "Sessão de pilates".</summary>
    public string Tipo { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public StatusAgendamento Status { get; set; } = StatusAgendamento.Pendente;
}
