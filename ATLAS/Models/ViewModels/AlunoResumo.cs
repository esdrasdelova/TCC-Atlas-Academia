namespace ATLAS.Models.ViewModels;

/// <summary>
/// Resumo de um aluno para as listagens da área do personal e do ADM.
/// Futuramente montado a partir da entidade Aluno do banco de dados.
/// </summary>
public class AlunoResumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public string TreinoAtual { get; set; } = "Sem treino atribuído";
    public bool ContaAtiva { get; set; } = true;
}