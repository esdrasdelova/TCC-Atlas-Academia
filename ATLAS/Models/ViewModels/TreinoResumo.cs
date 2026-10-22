namespace ATLAS.Models.ViewModels;

/// <summary>
/// Resumo de um treino do aluno para exibição. Futuramente montado a partir
/// das entidades Treino/TreinoExercicio carregadas do banco de dados.
/// </summary>
public class TreinoResumo
{
    public string Nome { get; set; } = string.Empty;
    public string Foco { get; set; } = string.Empty;
    public List<string> Exercicios { get; set; } = new();
    public bool Ativo { get; set; }
}

/// <summary>
/// Dados exibidos no perfil do aluno. Futuramente preenchido pela entidade Aluno.
/// </summary>
public class AlunoPerfilViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string Personal { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string Plano { get; set; } = string.Empty;
    public string MembroDesde { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
}