using ATLAS.Models.Entities;
using ATLAS.Models.Enums;

namespace ATLAS.Services;

/// <summary>Resultado da tentativa de atualizar o status de um agendamento.</summary>
public enum AtualizacaoAgendamentoResultado
{
    Ok,
    NaoEncontrado,
    SemPermissao,
    TransicaoInvalida
}

/// <summary>
/// Regras de negócio dos agendamentos (avaliações, aulas experimentais e
/// sessões com personal), com escopo por perfil:
///
///   - Aluno: cria/cancela apenas os próprios agendamentos;
///   - Personal: confirma/conclui/cancela agendamentos que sejam dele ou de
///     um aluno da sua carteira (Aluno.PersonalId);
///   - Administrador: acesso irrestrito.
/// </summary>
public interface IAgendamentoService
{
    /// <summary>Agendamentos do aluno (todos os status, mais recentes primeiro).</summary>
    Task<List<Agendamento>> ListarDoAlunoAsync(int alunoId);

    /// <summary>
    /// Agendamentos visíveis ao personal: apontados diretamente para ele ou
    /// de alunos da sua carteira (Aluno.PersonalId == personalId).
    /// </summary>
    Task<List<Agendamento>> ListarDoPersonalAsync(int personalId);

    /// <summary>Todos os agendamentos (uso administrativo).</summary>
    Task<List<Agendamento>> ListarTodosAsync();

    Task<Agendamento?> ObterAsync(int id);

    /// <summary>Cria um agendamento vinculado ao aluno informado.</summary>
    Task<int> CriarAsync(int alunoId, string tipo, DateTime dataHora, int? personalId);

    /// <summary>
    /// Aplica uma mudança de status respeitando o escopo do perfil e as
    /// transições válidas (Pendente→Confirmado/Cancelado, Confirmado→Concluído/Cancelado).
    /// </summary>
    Task<AtualizacaoAgendamentoResultado> AtualizarStatusAsync(
        int id,
        StatusAgendamento novoStatus,
        int? alunoId = null,
        int? personalId = null,
        bool admin = false);
}