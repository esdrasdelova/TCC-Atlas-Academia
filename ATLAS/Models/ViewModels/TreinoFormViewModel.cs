namespace ATLAS.Models.ViewModels;

/// <summary>
/// Dados do formulário de criação/edição de treino na área do personal.
/// Cada linha de exercício é vinculada por índice (Exercicios[0], Exercicios[1]…).
/// </summary>
public class TreinoFormViewModel
{
    public int Id { get; set; }

    public int AlunoId { get; set; }

    /// <summary>
    /// Personal responsável — usado pelo ADM ao criar/editar treinos.
    /// Na área do personal, o responsável é sempre o próprio usuário logado.
    /// </summary>
    public int PersonalId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Objetivo { get; set; } = "Hipertrofia";

    public string? Observacoes { get; set; }

    public bool Publicado { get; set; } = true;

    public List<TreinoExercicioInput> Exercicios { get; set; } = new();
}

public class TreinoExercicioInput
{
    public int ExercicioId { get; set; }

    /// <summary>Posição do exercício no treino — reordenável pelo personal.</summary>
    public int Ordem { get; set; }

    public int Series { get; set; } = 3;

    public string Repeticoes { get; set; } = string.Empty;

    public string? Carga { get; set; }

    public int DescansoSegundos { get; set; } = 60;

    public string? Observacoes { get; set; }
}

/// <summary>Linha do select de exercícios do catálogo.</summary>
public class ExercicioOpcao
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string GrupoMuscular { get; set; } = string.Empty;
}