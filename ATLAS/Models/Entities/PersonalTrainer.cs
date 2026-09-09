namespace ATLAS.Models.Entities;

public class PersonalTrainer : Usuario
{
    /// <summary>Registro de credenciamento profissional (ex.: CREF).</summary>
    public string? RegistroProfissional { get; set; }

    public string? Especialidade { get; set; }

    public ICollection<Aluno> Alunos { get; set; } = new List<Aluno>();

    public ICollection<Treino> TreinosCriados { get; set; } = new List<Treino>();
}
