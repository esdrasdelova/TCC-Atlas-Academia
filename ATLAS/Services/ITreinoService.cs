using ATLAS.Models;
using ATLAS.Models.Entities;

using ATLAS.Models.ViewModels;
namespace ATLAS.Services;

/// <summary>
/// Operações de treinos personalizados: o personal cria/edita/exclui treinos
/// por aluno; o aluno apenas consulta. Os controllers filtram pelos IDs do
/// usuário logado (AlunoId / PersonalId).
/// </summary>
public interface ITreinoService
{
    Task<List<Aluno>> ListarAlunosAsync(bool somenteAtivos = true);

    /// <summary>Alunos vinculados a um personal específico.</summary>
    Task<List<Aluno>> ListarAlunosDoPersonalAsync(int personalId, bool somenteAtivos = true);

    Task<List<ExercicioOpcao>> ListarCatalogoExerciciosAsync();
    Task<List<Treino>> ListarTreinosDoAlunoAsync(int alunoId);

    /// <summary>Todos os treinos (uso administrativo), com aluno e itens carregados.</summary>
    Task<List<Treino>> ListarTodosComAlunoAsync();

    /// <summary>Treinos criados por um personal específico.</summary>
    Task<List<Treino>> ListarTreinosDoPersonalAsync(int personalId);

    Task<Treino?> ObterTreinoCompletoAsync(int treinoId);

    Task<int> CriarAsync(TreinoFormViewModel form, int personalId);

    /// <summary>personalId opcional: quando informado, troca o responsável pelo treino.</summary>
    Task<bool> AtualizarAsync(int treinoId, TreinoFormViewModel form, int? personalId = null);

    Task<bool> ExcluirAsync(int treinoId);
    Task<bool> AlternarPublicacaoAsync(int treinoId);

    /// <summary>
    /// Marca um treino (publicado ou não) como o em uso pelo aluno:
    /// mantém publicado, define Ativo = true e desativa os demais treinos do aluno.
    /// </summary>
    Task<bool> AtivarAsync(int treinoId);
}
