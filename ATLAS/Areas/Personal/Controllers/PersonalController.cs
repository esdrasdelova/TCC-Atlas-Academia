using System.Security.Claims;
using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.Entities;
using ATLAS.Models.ViewModels;
using ATLAS.Models.Enums;
using ATLAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntidadeAluno = ATLAS.Models.Entities.Aluno;

namespace ATLAS.Areas.Personal.Controllers;

/// <summary>
/// Área exclusiva do personal trainer. Protegida pelo papel "Personal" e
/// restrita aos alunos vinculados (Aluno.PersonalId) e aos treinos sob a
/// responsabilidade do profissional autenticado.
/// </summary>
[Authorize(Roles = Permissoes.Personal)]
[Area("Personal")]
[Route("personal")]
public class PersonalController : Controller
{
    private readonly ITreinoService _treinos;
    private readonly AtlasDbContext _db;

    public PersonalController(ITreinoService treinos, AtlasDbContext db)
    {
        _treinos = treinos;
        _db = db;
    }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void PrepararViewData(string titulo)
    {
        ViewData["Titulo"] = titulo;
        ViewData["UsuarioNome"] = User.Identity?.Name ?? "Personal";
        ViewData["UsuarioPapel"] = Permissoes.Personal;
        ViewData["Sidebar"] = "_SidebarPersonal";
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Área do Personal";
        PrepararViewData("Área do Personal");

        var alunos = await _db.Alunos.AsNoTracking()
            .Where(a => a.PersonalId == UsuarioId)
            .Include(a => a.Treinos)
            .ToListAsync();

        var totalTreinos = await _db.Treinos.CountAsync(t => t.PersonalId == UsuarioId);

        ViewBag.TotalAlunos = alunos.Count(a => a.Status == StatusConta.Ativa);
        ViewBag.TotalTreinos = totalTreinos;
        ViewBag.AlunosSemTreino = alunos.Count(a =>
            a.Status == StatusConta.Ativa &&
            !a.Treinos.Any(t => t.Ativo && t.Publicado));

        return View();
    }

    [HttpGet("alunos")]
    public async Task<IActionResult> Alunos()
    {
        ViewData["Title"] = "Meus Alunos";
        PrepararViewData("Meus Alunos");

        var alunos = await _db.Alunos.AsNoTracking()
            .Where(a => a.PersonalId == UsuarioId)
            .Include(a => a.Treinos)
            .OrderBy(a => a.NomeCompleto)
            .ToListAsync();

        return View(alunos);
    }

    [HttpGet("alunos/{id:int}")]
    public async Task<IActionResult> Detalhes(int id)
    {
        var aluno = await _db.Alunos.AsNoTracking()
            .Include(a => a.Treinos).ThenInclude(t => t.Itens)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aluno == null || !AlunoEhMeu(aluno)) return NotFound();

        ViewData["Title"] = $"Aluno: {aluno.NomeCompleto}";
        PrepararViewData("Perfil do Aluno");
        return View(aluno);
    }

    /// <summary>O aluno é atendido por este personal?</summary>
    private bool AlunoEhMeu(EntidadeAluno aluno) => aluno.PersonalId == UsuarioId;

    [HttpGet("treinos")]
    public async Task<IActionResult> Treinos()
    {
        ViewData["Title"] = "Gerenciar Treinos";
        PrepararViewData("Gerenciar Treinos");

        var lista = await _treinos.ListarTreinosDoPersonalAsync(UsuarioId);
        ViewBag.AlunosAtivos = await _db.Alunos.CountAsync(a => a.PersonalId == UsuarioId && a.Status == StatusConta.Ativa);
        return View(lista);
    }

    [HttpGet("treinos/novo")]
    public async Task<IActionResult> CriarTreino([FromQuery] int? alunoId)
    {
        ViewData["Title"] = "Novo Treino";
        PrepararViewData("Novo Treino");

        var form = new TreinoFormViewModel();
        if (alunoId.HasValue) form.AlunoId = alunoId.Value;

        await PreencherOpcoesAsync(form);
        return View("CriarTreino", form);
    }

    [HttpPost("treinos/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarTreino(TreinoFormViewModel form)
    {
        if (!await FormularioValidoAsync(form))
        {
            await PreencherOpcoesAsync(form);
            return View(form);
        }

        var id = await _treinos.CriarAsync(form, UsuarioId);
        TempData["Aviso"] = $"Treino \"{form.Nome}\" publicado com sucesso! O aluno já consegue visualizá-lo na área dele.";
        return RedirectToAction(nameof(Detalhe), new { id });
    }

    [HttpGet("treinos/{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null || !await TreinoEhMeuAsync(treino)) return NotFound();

        ViewData["Title"] = $"{treino.Nome} · {treino.Aluno.NomeCompleto}";
        PrepararViewData($"Treino de {treino.Aluno.NomeCompleto.Split(' ')[0]}");
        return View(treino);
    }

    /// <summary>O treino está sob responsabilidade deste personal?</summary>
    private async Task<bool> TreinoEhMeuAsync(Treino treino)
    {
        if (treino.PersonalId == UsuarioId) return true;

        var alunoEhMeu = await _db.Alunos.AnyAsync(a => a.Id == treino.AlunoId && a.PersonalId == UsuarioId);
        return alunoEhMeu;
    }

    [HttpGet("treinos/{id:int}/editar")]
    public async Task<IActionResult> Editar(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null || !await TreinoEhMeuAsync(treino)) return NotFound();

        ViewData["Title"] = "Editar Treino";
        PrepararViewData("Editar Treino");

        var form = new TreinoFormViewModel
        {
            Id = treino.Id,
            AlunoId = treino.AlunoId,
            Nome = treino.Nome,
            Objetivo = treino.Objetivo ?? "Hipertrofia",
            Observacoes = treino.Observacoes,
            Publicado = treino.Publicado,
            Exercicios = treino.Itens.OrderBy(i => i.Ordem).Select(i => new TreinoExercicioInput
            {
                ExercicioId = i.ExercicioId,
                Ordem = i.Ordem,
                Series = i.Series,
                Repeticoes = i.Repeticoes,
                Carga = i.Carga,
                DescansoSegundos = i.DescansoSegundos,
                Observacoes = i.Observacoes
            }).ToList()
        };

        await PreencherOpcoesAsync(form);
        return View("EditarTreino", form);
    }

    [HttpPost("treinos/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, TreinoFormViewModel form)
    {
        var treinoAtual = await _treinos.ObterTreinoCompletoAsync(id);
        if (treinoAtual == null || !await TreinoEhMeuAsync(treinoAtual)) return NotFound();

        form.Id = id;
        if (!await FormularioValidoAsync(form))
        {
            await PreencherOpcoesAsync(form);
            return View("EditarTreino", form);
        }

        // O personal nunca troca o responsável pela ficha — mantém o atual.
        var ok = await _treinos.AtualizarAsync(id, form);
        if (!ok) return NotFound();

        TempData["Aviso"] = $"Treino \"{form.Nome}\" atualizado com sucesso!";
        return RedirectToAction(nameof(Detalhe), new { id });
    }

    [HttpPost("treinos/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null || !await TreinoEhMeuAsync(treino)) return NotFound();

        var ok = await _treinos.ExcluirAsync(id);
        TempData["Aviso"] = ok ? "Treino excluído." : "Não foi possível excluir o treino.";
        return RedirectToAction(nameof(Treinos));
    }

    [HttpPost("treinos/{id:int}/publicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarPublicacao(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null || !await TreinoEhMeuAsync(treino)) return NotFound();

        await _treinos.AlternarPublicacaoAsync(id);
        TempData["Aviso"] = "Publicação do treino atualizada.";
        return RedirectToAction(nameof(Treinos));
    }

    [HttpPost("treinos/{id:int}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ativar(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null || !await TreinoEhMeuAsync(treino)) return NotFound();

        await _treinos.AtivarAsync(id);
        TempData["Aviso"] = "Treino ativado como o em uso pelo aluno.";
        return RedirectToAction(nameof(Treinos));
    }

    /// <summary>Catálogo de exercícios agrupado por grupo muscular.</summary>
    [HttpGet("banco-exercicios")]
    public async Task<IActionResult> BancoExercicios(string? grupo)
    {
        ViewData["Title"] = "Banco de Exercícios";
        PrepararViewData("Banco de Exercícios");

        var exercicios = await _db.Exercicios.AsNoTracking()
            .Where(e => e.Ativo)
            .OrderBy(e => e.GrupoMuscular)
            .ThenBy(e => e.Nome)
            .ToListAsync();

        ViewBag.Grupos = exercicios.Select(e => e.GrupoMuscular).Distinct().OrderBy(g => g).ToList();
        ViewBag.GrupoSelecionado = grupo;

        if (!string.IsNullOrWhiteSpace(grupo))
        {
            exercicios = exercicios.Where(e => e.GrupoMuscular == grupo).ToList();
        }

        return View(exercicios);
    }

    /// <summary>Validações além das anotações do modelo.</summary>
    private async Task<bool> FormularioValidoAsync(TreinoFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Nome))
        {
            ModelState.AddModelError(nameof(form.Nome), "Informe o nome do treino.");
        }

        if (form.Exercicios.Count(e => e.ExercicioId > 0) == 0)
        {
            ModelState.AddModelError(string.Empty, "Adicione pelo menos um exercício ao treino.");
        }

        if (!await AlunoEhMeuAsync(form.AlunoId))
        {
            ModelState.AddModelError(nameof(form.AlunoId), "Selecione um aluno válido da sua carteira.");
        }

        return ModelState.IsValid;
    }

    private Task<bool> AlunoEhMeuAsync(int alunoId) =>
        _db.Alunos.AnyAsync(a => a.Id == alunoId && a.PersonalId == UsuarioId);

    private async Task PreencherOpcoesAsync(TreinoFormViewModel form)
    {
        ViewBag.CatalogoExercicios = await _treinos.ListarCatalogoExerciciosAsync();
        var alunos = await _treinos.ListarAlunosDoPersonalAsync(UsuarioId);
        ViewBag.AlunosSelect = alunos
            .OrderBy(a => a.NomeCompleto)
            .Select(a => new OpcaoSelect
            {
                Id = a.Id,
                Texto = $"{a.NomeCompleto} — {(string.IsNullOrEmpty(a.Objetivo) ? "sem objetivo" : a.Objetivo)}"
            })
            .ToList();
        ViewBag.FormArea = "Personal";
        ViewBag.FormController = "Personal";
        ViewBag.CancelarUrl = form.Id > 0
            ? Url.Action(nameof(Detalhe), new { id = form.Id })
            : Url.Action(nameof(Treinos));
    }
}
