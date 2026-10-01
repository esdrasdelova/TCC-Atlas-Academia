using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.Entities;
using ATLAS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Services;

public class TreinoService : ITreinoService
{
    private readonly AtlasDbContext _db;

    public TreinoService(AtlasDbContext db)
    {
        _db = db;
    }

    public Task<List<Aluno>> ListarAlunosAsync(bool somenteAtivos = true)
    {
        var query = _db.Alunos.AsNoTracking().OrderBy(a => a.NomeCompleto);
        return (somenteAtivos ? query.Where(a => a.Status == Models.Enums.StatusConta.Ativa) : query).ToListAsync();
    }

    public Task<List<Aluno>> ListarAlunosDoPersonalAsync(int personalId, bool somenteAtivos = true)
    {
        var query = _db.Alunos
            .AsNoTracking()
            .Where(a => a.PersonalId == personalId)
            .OrderBy(a => a.NomeCompleto);

        return (somenteAtivos ? query.Where(a => a.Status == Models.Enums.StatusConta.Ativa) : query).ToListAsync();
    }

    public async Task<List<ExercicioOpcao>> ListarCatalogoExerciciosAsync()
    {
        return await _db.Exercicios
            .AsNoTracking()
            .Where(e => e.Ativo)
            .OrderBy(e => e.GrupoMuscular).ThenBy(e => e.Nome)
            .Select(e => new ExercicioOpcao { Id = e.Id, Nome = e.Nome, GrupoMuscular = e.GrupoMuscular })
            .ToListAsync();
    }

    public Task<List<Treino>> ListarTreinosDoAlunoAsync(int alunoId)
    {
        return _db.Treinos
            .AsNoTracking()
            .Include(t => t.Itens).ThenInclude(i => i.Exercicio)
            .Where(t => t.AlunoId == alunoId && t.Publicado)
            .OrderByDescending(t => t.Ativo).ThenByDescending(t => t.DataCriacao)
            .ToListAsync();
    }

    public Task<List<Treino>> ListarTodosComAlunoAsync()
    {
        return _db.Treinos
            .AsNoTracking()
            .Include(t => t.Aluno)
            .Include(t => t.Personal)
            .Include(t => t.Itens)
            .OrderByDescending(t => t.DataCriacao)
            .ToListAsync();
    }

    public Task<List<Treino>> ListarTreinosDoPersonalAsync(int personalId)
    {
        return _db.Treinos
            .AsNoTracking()
            .Include(t => t.Aluno)
            .Include(t => t.Itens)
            .Where(t => t.PersonalId == personalId)
            .OrderByDescending(t => t.DataCriacao)
            .ToListAsync();
    }

    public Task<Treino?> ObterTreinoCompletoAsync(int treinoId)
    {
        return _db.Treinos
            .AsNoTracking()
            .Include(t => t.Aluno)
            .Include(t => t.Personal)
            .Include(t => t.Itens).ThenInclude(i => i.Exercicio)
            .FirstOrDefaultAsync(t => t.Id == treinoId);
    }

    public async Task<int> CriarAsync(TreinoFormViewModel form, int personalId)
    {
        var treino = new Treino
        {
            AlunoId = form.AlunoId,
            PersonalId = personalId,
            Nome = form.Nome.Trim(),
            Objetivo = form.Objetivo,
            Observacoes = Limpar(form.Observacoes),
            Publicado = form.Publicado,
            Ativo = form.Publicado
        };

        PreencherItens(treino, form);
        _db.Treinos.Add(treino);

        // Se o novo treino entra em uso, os anteriores do mesmo aluno viram alternativos.
        if (treino.Publicado && treino.Ativo)
        {
            await DesativarOutrosAsync(form.AlunoId, null);
        }

        await _db.SaveChangesAsync();
        return treino.Id;
    }

    public async Task<bool> AtualizarAsync(int treinoId, TreinoFormViewModel form, int? personalId = null)
    {
        var treino = await _db.Treinos
            .Include(t => t.Itens)
            .FirstOrDefaultAsync(t => t.Id == treinoId);

        if (treino == null)
        {
            return false;
        }

        treino.AlunoId = form.AlunoId;
        treino.Nome = form.Nome.Trim();
        treino.Objetivo = form.Objetivo;
        treino.Observacoes = Limpar(form.Observacoes);
        treino.Publicado = form.Publicado;

        if (personalId.HasValue && personalId.Value != treino.PersonalId)
        {
            var existePersonal = await _db.Personais.AnyAsync(p => p.Id == personalId.Value);
            if (existePersonal)
            {
                treino.PersonalId = personalId.Value;
            }
        }

        _db.TreinoExercicios.RemoveRange(treino.Itens);
        PreencherItens(treino, form);

        if (treino.Publicado && treino.Ativo)
        {
            await DesativarOutrosAsync(treino.AlunoId, treinoId);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExcluirAsync(int treinoId)
    {
        var treino = await _db.Treinos.FindAsync(treinoId);
        if (treino == null)
        {
            return false;
        }

        _db.Treinos.Remove(treino);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AlternarPublicacaoAsync(int treinoId)
    {
        var treino = await _db.Treinos.FindAsync(treinoId);
        if (treino == null)
        {
            return false;
        }

        treino.Publicado = !treino.Publicado;
        if (!treino.Publicado)
        {
            treino.Ativo = false;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AtivarAsync(int treinoId)
    {
        var treino = await _db.Treinos.FindAsync(treinoId);
        if (treino == null)
        {
            return false;
        }

        treino.Publicado = true;
        treino.Ativo = true;

        // Apenas um treino em uso por aluno: os demais voltam a ser alternativos.
        await DesativarOutrosAsync(treino.AlunoId, treinoId);

        await _db.SaveChangesAsync();
        return true;
    }

    private static void PreencherItens(Treino treino, TreinoFormViewModel form)
    {
        var ordem = 1;
        foreach (var item in form.Exercicios.Where(e => e.ExercicioId > 0))
        {
            treino.Itens.Add(new TreinoExercicio
            {
                ExercicioId = item.ExercicioId,
                Ordem = ordem++,
                Series = Math.Max(1, item.Series),
                Repeticoes = string.IsNullOrWhiteSpace(item.Repeticoes) ? "—" : item.Repeticoes.Trim(),
                Carga = Limpar(item.Carga),
                DescansoSegundos = Math.Max(0, item.DescansoSegundos),
                Observacoes = Limpar(item.Observacoes)
            });
        }
    }

    /// <summary>Mantém apenas um treino ativo por aluno.</summary>
    private async Task DesativarOutrosAsync(int alunoId, int? excetoTreinoId)
    {
        var outros = await _db.Treinos
            .Where(t => t.AlunoId == alunoId && t.Ativo && (excetoTreinoId == null || t.Id != excetoTreinoId))
            .ToListAsync();

        foreach (var outro in outros)
        {
            outro.Ativo = false;
        }
    }

    private static string? Limpar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
