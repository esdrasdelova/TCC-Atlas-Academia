using ATLAS.Data;
using ATLAS.Models.Entities;
using ATLAS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Services;

public class AgendamentoService : IAgendamentoService
{
    private readonly AtlasDbContext _db;

    public AgendamentoService(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<List<Agendamento>> ListarDoAlunoAsync(int alunoId) =>
        _db.Agendamentos
            .AsNoTracking()
            .Include(a => a.Personal)
            .Where(a => a.AlunoId == alunoId)
            .OrderByDescending(a => a.DataHora)
            .ToListAsync();

    public Task<List<Agendamento>> ListarDoPersonalAsync(int personalId) =>
        _db.Agendamentos
            .AsNoTracking()
            .Include(a => a.Aluno)
            .Include(a => a.Personal)
            .Where(a =>
                a.PersonalId == personalId ||
                a.Aluno.PersonalId == personalId)
            .OrderByDescending(a => a.DataHora)
            .ToListAsync();

    public Task<List<Agendamento>> ListarTodosAsync() =>
        _db.Agendamentos
            .AsNoTracking()
            .Include(a => a.Aluno)
            .Include(a => a.Personal)
            .OrderByDescending(a => a.DataHora)
            .ToListAsync();

    public Task<Agendamento?> ObterAsync(int id) =>
        _db.Agendamentos
            .AsNoTracking()
            .Include(a => a.Aluno)
            .Include(a => a.Personal)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<int> CriarAsync(int alunoId, string tipo, DateTime dataHora, int? personalId)
    {
        var agendamento = new Agendamento
        {
            AlunoId = alunoId,
            PersonalId = personalId,
            Tipo = tipo.Trim(),
            DataHora = dataHora,
            Status = StatusAgendamento.Pendente
        };

        _db.Agendamentos.Add(agendamento);
        await _db.SaveChangesAsync();
        return agendamento.Id;
    }

    public async Task<AtualizacaoAgendamentoResultado> AtualizarStatusAsync(
        int id, StatusAgendamento novoStatus, int? alunoId = null, int? personalId = null, bool admin = false)
    {
        var agendamento = await _db.Agendamentos
            .Include(a => a.Aluno)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agendamento == null)
        {
            return AtualizacaoAgendamentoResultado.NaoEncontrado;
        }

        // Modelos sem privilégio de admin precisam ter relação com o agendamento.
        if (!admin)
        {
            var temAcesso = personalId.HasValue
                ? agendamento.PersonalId == personalId || agendamento.Aluno.PersonalId == personalId
                : alunoId.HasValue && agendamento.AlunoId == alunoId;

            if (!temAcesso)
            {
                return AtualizacaoAgendamentoResultado.SemPermissao;
            }
        }

        // Mesmo status é idempotente (não quebra fluxos de retry).
        if (agendamento.Status == novoStatus)
        {
            return AtualizacaoAgendamentoResultado.Ok;
        }

        if (!TransicaoValida(agendamento.Status, novoStatus))
        {
            return AtualizacaoAgendamentoResultado.TransicaoInvalida;
        }

        agendamento.Status = novoStatus;
        await _db.SaveChangesAsync();
        return AtualizacaoAgendamentoResultado.Ok;
    }

    /// <summary>
    /// Máquina de estados do agendamento. Estados terminais (Concluído/
    /// Cancelado) não mudam mais.
    /// </summary>
    private static bool TransicaoValida(StatusAgendamento atual, StatusAgendamento novo) => atual switch
    {
        StatusAgendamento.Pendente => novo is StatusAgendamento.Confirmado or StatusAgendamento.Cancelado,
        StatusAgendamento.Confirmado => novo is StatusAgendamento.Concluido or StatusAgendamento.Cancelado,
        _ => false
    };
}