namespace ATLAS.Models.ViewModels;

/// <summary>
/// Dados do dashboard do aluno logado — montados no controller a partir do banco.
/// </summary>
public class AlunoDashboardViewModel
{
    public bool TemTreino { get; set; }

    public string TreinoAtualNome { get; set; } = "Sem treino ativo";

    public string TreinoAtualFoco { get; set; } = "Aguarde seu personal montar sua ficha.";

    public int TreinoAtualExercicios { get; set; }

    public DateTime? TreinoAtualizadoEm { get; set; }

    public string PersonalNome { get; set; } = "A definir";

    public string? PersonalRegistro { get; set; }

    public DateTime? ProximaAvaliacao { get; set; }

    public string? Objetivo { get; set; }
}