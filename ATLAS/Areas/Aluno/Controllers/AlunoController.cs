using System.Security.Claims;
using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.ViewModels;
using ATLAS.Models.Enums;
using ATLAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Areas.Aluno.Controllers;

/// <summary>
/// Área exclusiva do aluno. Protegida por papel — o usuário logado só vê
/// os próprios dados, carregados do banco pelo Id da sessão (claim).
/// </summary>
[Area("Aluno")]
[Route("aluno")]
[Authorize(Roles = Permissoes.Aluno)]
public class AlunoController : Controller
{
    private readonly ITreinoService _treinos;
    private readonly AtlasDbContext _db;

    public AlunoController(ITreinoService treinos, AtlasDbContext db)
    {
        _treinos = treinos;
        _db = db;
    }

    /// <summary>Id e nome vêm dos claims de autenticação.</summary>
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void PrepararViewData(string titulo)
    {
        ViewData["Titulo"] = titulo;
        ViewData["UsuarioNome"] = User.Identity?.Name ?? "Aluno";
        ViewData["UsuarioPapel"] = Permissoes.Aluno;
        ViewData["Sidebar"] = "_SidebarAluno";
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Área do Aluno";
        PrepararViewData("Área do Aluno");

        var aluno = await CarregarAlunoAsync();
        if (aluno == null)
        {
            TempData["Aviso"] = "Conta não encontrada no banco de dados.";
            return View(new AlunoDashboardViewModel());
        }

        var treinoAtivo = await _db.Treinos
            .AsNoTracking()
            .Include(t => t.Itens)
            .Where(t => t.AlunoId == aluno.Id && t.Ativo && t.Publicado)
            .OrderByDescending(t => t.DataCriacao)
            .FirstOrDefaultAsync();

        var modelo = new AlunoDashboardViewModel
        {
            TemTreino = treinoAtivo != null,
            TreinoAtualNome = treinoAtivo?.Nome ?? "Sem treino ativo",
            TreinoAtualFoco = treinoAtivo?.Objetivo ?? "Aguarde seu personal montar sua ficha.",
            TreinoAtualExercicios = treinoAtivo?.Itens.Count ?? 0,
            TreinoAtualizadoEm = treinoAtivo?.DataCriacao,
            PersonalNome = aluno.Personal?.NomeCompleto ?? "A definir",
            PersonalRegistro = aluno.Personal?.RegistroProfissional ?? "sem registro informado",
            ProximaAvaliacao = aluno.ProximaAvaliacao,
            Objetivo = aluno.Objetivo
        };

        return View(modelo);
    }

    /// <summary>Carrega o aluno logado com o personal incluído.</summary>
    private Task<Models.Entities.Aluno?> CarregarAlunoAsync() =>
        _db.Alunos.AsNoTracking()
            .Include(a => a.Personal)
            .FirstOrDefaultAsync(a => a.Id == UsuarioId);

    [HttpGet("treinos")]
    public async Task<IActionResult> Treinos()
    {
        ViewData["Title"] = "Meus Treinos";
        PrepararViewData("Meus Treinos");

        var lista = await _treinos.ListarTreinosDoAlunoAsync(UsuarioId);
        ViewBag.TotalTreinos = lista.Count;

        return View(lista);
    }

    [HttpGet("treinos/{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);

        // O aluno só visualiza treinos publicados que pertencem a ele.
        if (treino == null || treino.AlunoId != UsuarioId || !treino.Publicado)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Treino: {treino.Nome}";
        PrepararViewData(treino.Nome);
        return View(treino);
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        ViewData["Title"] = "Meu Perfil";
        PrepararViewData("Meu Perfil");

        var aluno = await CarregarAlunoAsync();
        if (aluno == null)
        {
            TempData["Aviso"] = "Conta não encontrada no banco de dados.";
            return RedirectToAction(nameof(Index));
        }

        var perfil = new AlunoPerfilViewModel
        {
            Nome = aluno.NomeCompleto,
            Email = aluno.Email,
            Telefone = string.IsNullOrEmpty(aluno.Telefone) ? "" : aluno.Telefone,
            DataNascimento = aluno.DataNascimento == default ? null : aluno.DataNascimento,
            Personal = aluno.Personal == null
                ? "A definir pela administração"
                : $"{aluno.Personal.NomeCompleto} · CREF {aluno.Personal.RegistroProfissional}",
            Objetivo = aluno.Objetivo ?? "",
            Plano = aluno.Status == StatusConta.Ativa ? "Ativo" : "Inativo",
            MembroDesde = aluno.CriadoEm.ToString("MMMM 'de' yyyy"),
            FotoUrl = aluno.FotoUrl
        };

        return View(perfil);
    }

    [HttpPost("perfil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarPerfil(
        string nomeCompleto, string telefone, string? objetivo)
    {
        var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == UsuarioId);
        if (aluno == null)
        {
            TempData["Aviso"] = "Conta não encontrada no banco de dados.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(nomeCompleto))
            aluno.NomeCompleto = nomeCompleto.Trim();

        if (!string.IsNullOrWhiteSpace(telefone))
            aluno.Telefone = telefone.Trim();

        aluno.Objetivo = string.IsNullOrWhiteSpace(objetivo) ? null : objetivo.Trim();

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Perfil atualizado com sucesso!";
        return RedirectToAction(nameof(Perfil));
    }

    [HttpPost("perfil/foto")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarFoto(IFormFile foto)
    {
        if (foto == null || foto.Length == 0)
        {
            TempData["Aviso"] = "Selecione uma imagem para atualizar.";
            return RedirectToAction(nameof(Perfil));
        }

        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();

        if (!extensoesPermitidas.Contains(ext))
        {
            TempData["Aviso"] = "Formato não permitido. Use JPG, PNG ou WebP.";
            return RedirectToAction(nameof(Perfil));
        }

        if (foto.Length > 5 * 1024 * 1024)
        {
            TempData["Aviso"] = "A imagem deve ter no máximo 5 MB.";
            return RedirectToAction(nameof(Perfil));
        }

        var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == UsuarioId);
        if (aluno == null)
        {
            TempData["Aviso"] = "Conta não encontrada no banco de dados.";
            return RedirectToAction(nameof(Index));
        }

        var pasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "fotos");
        Directory.CreateDirectory(pasta);

        var nomeArquivo = $"{aluno.Id}{ext}";
        var caminho = Path.Combine(pasta, nomeArquivo);

        using (var stream = new FileStream(caminho, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        aluno.FotoUrl = $"/uploads/fotos/{nomeArquivo}";
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Foto atualizada com sucesso!";
        return RedirectToAction(nameof(Perfil));
    }
}
