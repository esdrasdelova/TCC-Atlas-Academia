using ATLAS.Models.Enums;

namespace ATLAS.Models.Entities;

public class Aluno : Usuario
{
    /// <summary>Personal responsável pelo aluno (Aluno → Personal → Treinos).</summary>
    public int? PersonalId { get; set; }
    public PersonalTrainer? Personal { get; set; }

    public string? Objetivo { get; set; }

    public string? FotoUrl { get; set; }

    public DateTime? ProximaAvaliacao { get; set; }

    public ICollection<Treino> Treinos { get; set; } = new List<Treino>();

    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
}
