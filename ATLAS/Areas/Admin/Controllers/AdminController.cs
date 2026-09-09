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

namespace ATLAS.Areas.Admin.Controllers;

/// <summary>
/// Painel administrativo: dashboard com dados reais do banco e gestão completa
/// de alunos, personais (professores) e treinos personalizados. Todas as ações
/// exigem o papel Administrador — validado por cookie de autenticação.
/// </summary>
[Authorize(Roles = Permissoes.Administrador)]
[Area("Admin")]
[Route("admin")]
public class AdminController : Controller
{
    private readonly AtlasDbContext _db;
    private readonly ITreinoService _treinos;
    private readonly ISiteConfigService _siteConfig;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AtlasDbContext db, ITreinoService treinos, ISiteConfigService siteConfig, ILogger<AdminController> logger)
    {
        _db = db;
        _treinos = treinos;
        _siteConfig = siteConfig;
        _logger = logger;
    }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void PrepararViewData(string titulo)
    {
        ViewData["Titulo"] = titulo;
        ViewData["UsuarioNome"] = User.Identity?.Name ?? "Administrador";
        ViewData["UsuarioPapel"] = Permissoes.Administrador;
        ViewData["Sidebar"] = "_SidebarAdmin";
    }

    // ---------------------------------------------------------------------
    // DASHBOARD
    // ---------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Painel Administrativo";
        PrepararViewData("Visão Geral");

        var usuarios = await _db.Usuarios.AsNoTracking().ToListAsync();
        var modelo = new AdminDashboardViewModel
        {
            TotalAlunos = usuarios.OfType<EntidadeAluno>().Count(),
            AlunosAtivos = usuarios.OfType<EntidadeAluno>().Count(a => a.Status == StatusConta.Ativa),
            AlunosInativos = usuarios.OfType<EntidadeAluno>().Count(a => a.Status != StatusConta.Ativa),
            TotalPersonais = usuarios.OfType<PersonalTrainer>().Count(),
            TotalAdministradores = usuarios.OfType<Administrador>().Count(),
            TreinosPersonalizados = await _db.Treinos.CountAsync(t => t.Publicado),
            CadastrosRecentes = usuarios
                .OrderByDescending(u => u.CriadoEm)
                .Take(6)
                .Select(u => new CadastroResumo
                {
                    Id = u.Id,
                    Nome = u.NomeCompleto,
                    Papel = u switch
                    {
                        Administrador => Permissoes.Administrador,
                        PersonalTrainer => Permissoes.Personal,
                        _ => Permissoes.Aluno
                    },
                    Data = u.CriadoEm
                })
                .ToList()
        };

        return View(modelo);
    }

    // ---------------------------------------------------------------------
    // ALUNOS
    // ---------------------------------------------------------------------

    [HttpGet("alunos")]
    public async Task<IActionResult> Alunos(string? q)
    {
        ViewData["Title"] = "Alunos — Administração";
        PrepararViewData("Alunos");
        ViewBag.Busca = q;

        var lista = await MontarListaAlunosAsync(q);
        return View(lista);
    }

    private async Task<List<AlunoListItemViewModel>> MontarListaAlunosAsync(string? q)
    {
        var query = _db.Alunos.AsNoTracking()
            .Include(a => a.Personal)
            .Include(a => a.Treinos)
            .AsQueryable();

        var termo = (q ?? string.Empty).Trim().ToLower();
        if (termo.Length > 0)
        {
            query = query.Where(a => a.NomeCompleto.ToLower().Contains(termo)
                                  || a.Email.ToLower().Contains(termo));
        }

        var lista = await query.OrderBy(a => a.NomeCompleto).ToListAsync();

        return lista.Select(a => new AlunoListItemViewModel
        {
            Id = a.Id,
            NomeCompleto = a.NomeCompleto,
            Email = a.Email,
            Telefone = a.Telefone,
            Objetivo = a.Objetivo,
            PersonalNome = a.Personal?.NomeCompleto,
            ContaAtiva = a.Status == StatusConta.Ativa,
            CriadoEm = a.CriadoEm,
            QtdTreinos = a.Treinos.Count,
            NomeTreinoAtivo = a.Treinos.FirstOrDefault(t => t.Ativo && t.Publicado)?.Nome
        }).ToList();
    }

    [HttpGet("alunos/{id:int}")]
    public async Task<IActionResult> AlunoDetalhe(int id)
    {
        var aluno = await CarregarAlunoCompletoAsync(id);
        if (aluno == null) return NotFound();

        ViewData["Title"] = $"Aluno: {aluno.NomeCompleto}";
        PrepararViewData("Ficha do Aluno");
        return View(aluno);
    }

    private Task<EntidadeAluno?> CarregarAlunoCompletoAsync(int id) =>
        _db.Alunos.AsNoTracking()
            .Include(a => a.Personal)
            .Include(a => a.Treinos).ThenInclude(t => t.Itens)
            .FirstOrDefaultAsync(a => a.Id == id);

    [HttpGet("alunos/novo")]
    public async Task<IActionResult> AlunoNovo()
    {
        ViewData["Title"] = "Novo Aluno";
        PrepararViewData("Cadastrar Aluno");
        await PreencherPersonaisSelectAsync();
        return View("AlunoFormulario", new AlunoFormViewModel());
    }

    [HttpPost("alunos/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlunoNovo(AlunoFormViewModel form)
    {
        ViewData["Title"] = "Novo Aluno";
        PrepararViewData("Cadastrar Aluno");

        await ValidarFormularioAlunoAsync(form, ehEdicao: false);
        if (!ModelState.IsValid)
        {
            await PreencherPersonaisSelectAsync();
            return View("AlunoFormulario", form);
        }

        var aluno = new EntidadeAluno
        {
            NomeCompleto = form.NomeCompleto.Trim(),
            Email = form.Email.Trim().ToLower(),
            Telefone = LimparTexto(form.Telefone) ?? string.Empty,
            DataNascimento = form.DataNascimento,
            Objetivo = LimparTexto(form.Objetivo),
            PersonalId = form.PersonalId > 0 ? form.PersonalId : null,
            Status = form.ContaAtiva ? StatusConta.Ativa : StatusConta.Inativa,
            SenhaHash = SegurancaSenha.GerarHash(form.Senha!),
            CriadoEm = DateTime.UtcNow
        };

        _db.Alunos.Add(aluno);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Aluno \"{aluno.NomeCompleto}\" cadastrado com sucesso.";
        return RedirectToAction(nameof(AlunoDetalhe), new { id = aluno.Id });
    }

    [HttpGet("alunos/{id:int}/editar")]
    public async Task<IActionResult> AlunoEditar(int id)
    {
        var aluno = await _db.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (aluno == null) return NotFound();

        ViewData["Title"] = $"Editar Aluno: {aluno.NomeCompleto}";
        PrepararViewData("Editar Aluno");

        var form = new AlunoFormViewModel
        {
            Id = aluno.Id,
            NomeCompleto = aluno.NomeCompleto,
            Email = aluno.Email,
            Telefone = aluno.Telefone,
            DataNascimento = aluno.DataNascimento,
            Objetivo = aluno.Objetivo,
            PersonalId = aluno.PersonalId ?? 0,
            ContaAtiva = aluno.Status == StatusConta.Ativa
        };

        await PreencherPersonaisSelectAsync();
        return View("AlunoFormulario", form);
    }

    [HttpPost("alunos/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlunoEditar(int id, AlunoFormViewModel form)
    {
        ViewData["Title"] = "Editar Aluno";
        PrepararViewData("Editar Aluno");

        form.Id = id;
        await ValidarFormularioAlunoAsync(form, ehEdicao: true);
        if (!ModelState.IsValid)
        {
            await PreencherPersonaisSelectAsync();
            return View("AlunoFormulario", form);
        }

        var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == id);
        if (aluno == null) return NotFound();

        aluno.NomeCompleto = form.NomeCompleto.Trim();
        aluno.Email = form.Email.Trim().ToLower();
        aluno.Telefone = LimparTexto(form.Telefone) ?? string.Empty;
        aluno.DataNascimento = form.DataNascimento;
        aluno.Objetivo = LimparTexto(form.Objetivo);
        aluno.PersonalId = form.PersonalId > 0 ? form.PersonalId : null;
        aluno.Status = form.ContaAtiva ? StatusConta.Ativa : StatusConta.Inativa;

        if (!string.IsNullOrWhiteSpace(form.NovaSenha))
        {
            aluno.SenhaHash = SegurancaSenha.GerarHash(form.NovaSenha);
            _logger.LogWarning("ADM {AdmId} redefiniu a senha do aluno {AlunoId}.", UsuarioId, id);
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Dados de \"{aluno.NomeCompleto}\" atualizados.";
        return RedirectToAction(nameof(AlunoDetalhe), new { id });
    }

    [HttpPost("alunos/{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlunoAlternarStatus(int id)
    {
        var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == id);
        if (aluno == null) return NotFound();

        aluno.Status = aluno.Status == StatusConta.Ativa ? StatusConta.Inativa : StatusConta.Ativa;
        await _db.SaveChangesAsync();

        _logger.LogInformation("ADM {AdmId} alterou o status do aluno {AlunoId} para {Status}.",
            UsuarioId, id, aluno.Status);

        TempData["Sucesso"] = aluno.Status == StatusConta.Ativa
            ? $"Conta de \"{aluno.NomeCompleto}\" ativada."
            : $"Conta de \"{aluno.NomeCompleto}\" desativada — o aluno não conseguirá entrar.";

        return RedirectToAction(nameof(Alunos));
    }

    [HttpPost("alunos/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlunoExcluir(int id)
    {
        var aluno = await _db.Alunos
            .Include(a => a.Treinos)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aluno == null) return NotFound();

        // Exclusão em cascata controlada no código: remove primeiro os treinos
        // do aluno (e os itens são apagados pelo cascade do banco).
        _db.Treinos.RemoveRange(aluno.Treinos);
        _db.Alunos.Remove(aluno);
        await _db.SaveChangesAsync();

        _logger.LogWarning("ADM {AdmId} excluiu o aluno {AlunoId} e {QtdTreeinos} treino(s).",
            UsuarioId, id, aluno.Treinos.Count);

        TempData["Sucesso"] = $"Aluno \"{aluno.NomeCompleto}\" excluído junto com {aluno.Treinos.Count} treino(s).";
        return RedirectToAction(nameof(Alunos));
    }

    private async Task ValidarFormularioAlunoAsync(AlunoFormViewModel form, bool ehEdicao)
    {
        var email = (form.Email ?? string.Empty).Trim().ToLower();
        if (await EmailEmUsoAsync(email, form.Id, ehAluno: true))
        {
            ModelState.AddModelError(nameof(form.Email), "Já existe uma conta com este e-mail.");
        }

        if (form.DataNascimento == default
            || form.DataNascimento > DateTime.UtcNow.AddYears(-10)
            || form.DataNascimento < new DateTime(1920, 1, 1))
        {
            ModelState.AddModelError(nameof(form.DataNascimento),
                "Informe uma data de nascimento válida (mínimo 10 anos).");
        }

        if (!ehEdicao && string.IsNullOrWhiteSpace(form.Senha))
        {
            ModelState.AddModelError(nameof(form.Senha), "Defina uma senha inicial para o aluno.");
        }
        else if (!ehEdicao && !string.IsNullOrWhiteSpace(form.Senha) && form.Senha!.Length < 6)
        {
            ModelState.AddModelError(nameof(form.Senha), "A senha deve ter pelo menos 6 caracteres.");
        }

        if (ehEdicao && !string.IsNullOrWhiteSpace(form.NovaSenha) && form.NovaSenha!.Length < 6)
        {
            ModelState.AddModelError(nameof(form.NovaSenha), "A nova senha deve ter pelo menos 6 caracteres.");
        }
    }

    // ---------------------------------------------------------------------
    // PERSONAIS / PROFESSORES
    // ---------------------------------------------------------------------

    [HttpGet("personais")]
    public async Task<IActionResult> Personais(string? q)
    {
        ViewData["Title"] = "Profissionais — Administração";
        PrepararViewData("Profissionais (Personais)");
        ViewBag.Busca = q;

        var query = _db.Personais.AsNoTracking()
            .Include(p => p.Alunos)
            .Include(p => p.TreinosCriados)
            .AsQueryable();

        var termo = (q ?? string.Empty).Trim().ToLower();
        if (termo.Length > 0)
        {
            query = query.Where(p => p.NomeCompleto.ToLower().Contains(termo)
                                  || p.Email.ToLower().Contains(termo));
        }

        var lista = await query.OrderBy(p => p.NomeCompleto).ToListAsync();

        var modelos = lista.Select(p => new PersonalListItemViewModel
        {
            Id = p.Id,
            NomeCompleto = p.NomeCompleto,
            Email = p.Email,
            Telefone = p.Telefone,
            RegistroProfissional = p.RegistroProfissional,
            Especialidade = p.Especialidade,
            ContaAtiva = p.Status == StatusConta.Ativa,
            CriadoEm = p.CriadoEm,
            QtdAlunos = p.Alunos.Count,
            QtdTreinos = p.TreinosCriados.Count
        }).ToList();

        return View(modelos);
    }

    [HttpGet("personais/{id:int}")]
    public async Task<IActionResult> PersonalDetalhe(int id)
    {
        var personal = await _db.Personais.AsNoTracking()
            .Include(p => p.Alunos).ThenInclude(a => a.Treinos)
            .Include(p => p.TreinosCriados)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (personal == null) return NotFound();

        ViewData["Title"] = $"Profissional: {personal.NomeCompleto}";
        PrepararViewData("Ficha do Profissional");
        return View(personal);
    }

    [HttpGet("personais/novo")]
    public IActionResult PersonalNovo()
    {
        ViewData["Title"] = "Novo Profissional";
        PrepararViewData("Cadastrar Profissional");
        return View("PersonalFormulario", new PersonalFormViewModel());
    }

    [HttpPost("personais/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonalNovo(PersonalFormViewModel form)
    {
        ViewData["Title"] = "Novo Profissional";
        PrepararViewData("Cadastrar Profissional");

        await ValidarFormularioPersonalAsync(form, ehEdicao: false);
        if (!ModelState.IsValid)
        {
            return View("PersonalFormulario", form);
        }

        var personal = new PersonalTrainer
        {
            NomeCompleto = form.NomeCompleto.Trim(),
            Email = form.Email.Trim().ToLower(),
            Telefone = LimparTexto(form.Telefone) ?? string.Empty,
            DataNascimento = form.DataNascimento,
            RegistroProfissional = LimparTexto(form.RegistroProfissional),
            Especialidade = LimparTexto(form.Especialidade),
            Status = form.ContaAtiva ? StatusConta.Ativa : StatusConta.Inativa,
            SenhaHash = SegurancaSenha.GerarHash(form.Senha!),
            CriadoEm = DateTime.UtcNow
        };

        _db.Personais.Add(personal);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Profissional \"{personal.NomeCompleto}\" cadastrado com sucesso.";
        return RedirectToAction(nameof(PersonalDetalhe), new { id = personal.Id });
    }

    [HttpGet("personais/{id:int}/editar")]
    public async Task<IActionResult> PersonalEditar(int id)
    {
        var personal = await _db.Personais.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (personal == null) return NotFound();

        ViewData["Title"] = $"Editar Profissional: {personal.NomeCompleto}";
        PrepararViewData("Editar Profissional");

        var form = new PersonalFormViewModel
        {
            Id = personal.Id,
            NomeCompleto = personal.NomeCompleto,
            Email = personal.Email,
            Telefone = personal.Telefone,
            DataNascimento = personal.DataNascimento,
            RegistroProfissional = personal.RegistroProfissional,
            Especialidade = personal.Especialidade,
            ContaAtiva = personal.Status == StatusConta.Ativa
        };

        return View("PersonalFormulario", form);
    }

    [HttpPost("personais/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonalEditar(int id, PersonalFormViewModel form)
    {
        ViewData["Title"] = "Editar Profissional";
        PrepararViewData("Editar Profissional");

        form.Id = id;
        await ValidarFormularioPersonalAsync(form, ehEdicao: true);
        if (!ModelState.IsValid)
        {
            return View("PersonalFormulario", form);
        }

        var personal = await _db.Personais.FirstOrDefaultAsync(p => p.Id == id);
        if (personal == null) return NotFound();

        personal.NomeCompleto = form.NomeCompleto.Trim();
        personal.Email = form.Email.Trim().ToLower();
        personal.Telefone = LimparTexto(form.Telefone) ?? string.Empty;
        personal.DataNascimento = form.DataNascimento;
        personal.RegistroProfissional = LimparTexto(form.RegistroProfissional);
        personal.Especialidade = LimparTexto(form.Especialidade);
        personal.Status = form.ContaAtiva ? StatusConta.Ativa : StatusConta.Inativa;

        if (!string.IsNullOrWhiteSpace(form.NovaSenha))
        {
            personal.SenhaHash = SegurancaSenha.GerarHash(form.NovaSenha);
            _logger.LogWarning("ADM {AdmId} redefiniu a senha do personal {PersonalId}.", UsuarioId, id);
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Dados de \"{personal.NomeCompleto}\" atualizados.";
        return RedirectToAction(nameof(PersonalDetalhe), new { id });
    }

    [HttpPost("personais/{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonalAlternarStatus(int id)
    {
        var personal = await _db.Personais.FirstOrDefaultAsync(p => p.Id == id);
        if (personal == null) return NotFound();

        if (personal.Id == UsuarioId)
        {
            TempData["Erro"] = "Você não pode desativar a sua própria conta.";
            return RedirectToAction(nameof(Personais));
        }

        personal.Status = personal.Status == StatusConta.Ativa ? StatusConta.Inativa : StatusConta.Ativa;
        await _db.SaveChangesAsync();

        _logger.LogInformation("ADM {AdmId} alterou o status do personal {PersonalId} para {Status}.",
            UsuarioId, id, personal.Status);

        TempData["Sucesso"] = personal.Status == StatusConta.Ativa
            ? $"Conta de \"{personal.NomeCompleto}\" ativada."
            : $"Conta de \"{personal.NomeCompleto}\" desativada.";

        return RedirectToAction(nameof(Personais));
    }

    [HttpPost("personais/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonalExcluir(int id)
    {
        var personal = await _db.Personais
            .Include(p => p.Alunos)
            .Include(p => p.TreinosCriados)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (personal == null) return NotFound();

        if (personal.Id == UsuarioId)
        {
            TempData["Erro"] = "Você não pode excluir a sua própria conta enquanto estiver autenticado.";
            return RedirectToAction(nameof(Personais));
        }

        // Cascata controlada: treinos criados são removidos e os alunos ficam
        // sem personal responsável (podendo ser reatribuídos depois).
        foreach (var aluno in personal.Alunos)
        {
            aluno.PersonalId = null;
        }

        _db.Treinos.RemoveRange(personal.TreinosCriados);
        _db.Personais.Remove(personal);
        await _db.SaveChangesAsync();

        _logger.LogWarning("ADM {AdmId} excluiu o personal {PersonalId} ({QuantosTreinos} treinos e {QuantosAlunos} alunos liberados).",
            UsuarioId, id, personal.TreinosCriados.Count, personal.Alunos.Count);

        TempData["Sucesso"] = $"Profissional \"{personal.NomeCompleto}\" excluído. " +
            $"{personal.TreinosCriados.Count} treino(s) removido(s) e {personal.Alunos.Count} aluno(s) liberado(s) para reatribuição.";
        return RedirectToAction(nameof(Personais));
    }

    private async Task ValidarFormularioPersonalAsync(PersonalFormViewModel form, bool ehEdicao)
    {
        var email = (form.Email ?? string.Empty).Trim().ToLower();
        if (await EmailEmUsoAsync(email, form.Id, ehAluno: false))
        {
            ModelState.AddModelError(nameof(form.Email), "Já existe uma conta com este e-mail.");
        }

        if (form.DataNascimento == default || form.DataNascimento > DateTime.UtcNow.AddYears(-16))
        {
            ModelState.AddModelError(nameof(form.DataNascimento),
                "Informe uma data de nascimento válida (mínimo 16 anos).");
        }

        if (!ehEdicao && string.IsNullOrWhiteSpace(form.Senha))
        {
            ModelState.AddModelError(nameof(form.Senha), "Defina uma senha inicial para o profissional.");
        }
        else if (!ehEdicao && !string.IsNullOrWhiteSpace(form.Senha) && form.Senha!.Length < 6)
        {
            ModelState.AddModelError(nameof(form.Senha), "A senha deve ter pelo menos 6 caracteres.");
        }

        if (ehEdicao && !string.IsNullOrWhiteSpace(form.NovaSenha) && form.NovaSenha!.Length < 6)
        {
            ModelState.AddModelError(nameof(form.NovaSenha), "A nova senha deve ter pelo menos 6 caracteres.");
        }
    }

    private Task<bool> EmailEmUsoAsync(string email, int ignorarId, bool ehAluno) =>
        _db.Usuarios.AnyAsync(u =>
            u.Id != ignorarId &&
            u.Email.ToLower() == email &&
            (ehAluno ? u is EntidadeAluno : u is PersonalTrainer));

    private async Task PreencherPersonaisSelectAsync()
    {
        ViewBag.PersonaisSelect = await _db.Personais
            .AsNoTracking()
            .Where(p => p.Status == StatusConta.Ativa)
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new OpcaoSelect { Id = p.Id, Texto = p.NomeCompleto })
            .ToListAsync();
    }

    // ---------------------------------------------------------------------
    // TREINOS PERSONALIZADOS
    // ---------------------------------------------------------------------

    [HttpGet("treinos")]
    public async Task<IActionResult> Treinos()
    {
        ViewData["Title"] = "Treinos Personalizados — Administração";
        PrepararViewData("Treinos Personalizados");

        var lista = await _treinos.ListarTodosComAlunoAsync();
        ViewBag.TotalPublicados = lista.Count(t => t.Publicado);
        return View(lista);
    }

    [HttpGet("treinos/novo")]
    public async Task<IActionResult> TreinoNovo([FromQuery] int? alunoId)
    {
        ViewData["Title"] = "Novo Treino — Administração";
        PrepararViewData("Novo Treino");

        var form = new TreinoFormViewModel();
        if (alunoId.HasValue) form.AlunoId = alunoId.Value;

        await PreencherOpcoesTreinoAsync(form);
        return View(form);
    }

    [HttpPost("treinos/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TreinoNovo(TreinoFormViewModel form)
    {
        ViewData["Title"] = "Novo Treino — Administração";
        PrepararViewData("Novo Treino");

        if (!await FormularioTreinoValidoAsync(form))
        {
            await PreencherOpcoesTreinoAsync(form);
            return View(form);
        }

        var personalId = await ResolverPersonalDoTreinoAsync(form);
        var id = await _treinos.CriarAsync(form, personalId);

        TempData["Sucesso"] = $"Treino \"{form.Nome}\" criado com sucesso.";
        return RedirectToAction(nameof(TreinoDetalhe), new { id });
    }

    [HttpGet("treinos/{id:int}")]
    public async Task<IActionResult> TreinoDetalhe(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null) return NotFound();

        ViewData["Title"] = $"Treino: {treino.Nome}";
        PrepararViewData($"Treino de {treino.Aluno.NomeCompleto.Split(' ')[0]}");
        return View(treino);
    }

    [HttpGet("treinos/{id:int}/editar")]
    public async Task<IActionResult> TreinoEditar(int id)
    {
        var treino = await _treinos.ObterTreinoCompletoAsync(id);
        if (treino == null) return NotFound();

        ViewData["Title"] = "Editar Treino — Administração";
        PrepararViewData("Editar Treino");
        ViewBag.NomeAluno = treino.Aluno.NomeCompleto;

        var form = MapearFormularioTreino(treino);

        await PreencherOpcoesTreinoAsync(form);
        return View(form);
    }

    private static TreinoFormViewModel MapearFormularioTreino(Treino treino) => new()
    {
        Id = treino.Id,
        AlunoId = treino.AlunoId,
        PersonalId = treino.PersonalId,
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

    [HttpPost("treinos/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TreinoEditar(int id, TreinoFormViewModel form)
    {
        ViewData["Title"] = "Editar Treino — Administração";
        PrepararViewData("Editar Treino");

        form.Id = id;
        if (!await FormularioTreinoValidoAsync(form))
        {
            var treinoAtual = await _treinos.ObterTreinoCompletoAsync(id);
            ViewBag.NomeAluno = treinoAtual?.Aluno?.NomeCompleto ?? "";
            await PreencherOpcoesTreinoAsync(form);
            return View(form);
        }

        int? trocarPersonal = form.PersonalId > 0 ? form.PersonalId : null;
        var ok = await _treinos.AtualizarAsync(id, form, trocarPersonal);
        if (!ok) return NotFound();

        TempData["Sucesso"] = $"Treino \"{form.Nome}\" atualizado.";
        return RedirectToAction(nameof(TreinoDetalhe), new { id });
    }

    [HttpPost("treinos/{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TreinoExcluir(int id)
    {
        var ok = await _treinos.ExcluirAsync(id);
        TempData[ok ? "Sucesso" : "Erro"] = ok ? "Treino excluído." : "Treino não encontrado.";
        return RedirectToAction(nameof(Treinos));
    }

    [HttpPost("treinos/{id:int}/publicar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TreinoAlternarPublicacao(int id)
    {
        var ok = await _treinos.AlternarPublicacaoAsync(id);
        if (!ok) return NotFound();
        TempData["Sucesso"] = "Publicação do treino atualizada.";
        return RedirectToAction(nameof(Treinos));
    }

    [HttpPost("treinos/{id:int}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TreinoAtivar(int id)
    {
        var ok = await _treinos.AtivarAsync(id);
        if (!ok) return NotFound();
        TempData["Sucesso"] = "Treino ativado como o em uso pelo aluno.";
        return RedirectToAction(nameof(Treinos));
    }

    private async Task<bool> FormularioTreinoValidoAsync(TreinoFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.Nome))
        {
            ModelState.AddModelError(nameof(form.Nome), "Informe o nome do treino.");
        }

        if (form.Exercicios.Count(e => e.ExercicioId > 0) == 0)
        {
            ModelState.AddModelError(string.Empty, "Adicione pelo menos um exercício ao treino.");
        }

        if (!await _db.Alunos.AnyAsync(a => a.Id == form.AlunoId))
        {
            ModelState.AddModelError(nameof(form.AlunoId), "Selecione um aluno válido.");
        }

        return ModelState.IsValid;
    }

    /// <summary>
    /// Personal responsável pelo treino: usa o selecionado no formulário ou,
    /// na ausência, o personal atual do aluno; sem nenhum dos dois, o próprio ADM.
    /// </summary>
    private async Task<int> ResolverPersonalDoTreinoAsync(TreinoFormViewModel form)
    {
        if (form.PersonalId > 0 && await _db.Personais.AnyAsync(p => p.Id == form.PersonalId))
        {
            return form.PersonalId;
        }

        var personalDoAluno = await _db.Alunos
            .Where(a => a.Id == form.AlunoId)
            .Select(a => a.PersonalId)
            .FirstOrDefaultAsync();

        if (personalDoAluno.HasValue)
        {
            return personalDoAluno.Value;
        }

        return UsuarioId;
    }

    /// <summary>
    /// Alimenta selects e URLs usados pelo partial compartilhado _FormularioTreino.
    /// </summary>
    private async Task PreencherOpcoesTreinoAsync(TreinoFormViewModel form)
    {
        ViewBag.CatalogoExercicios = await _treinos.ListarCatalogoExerciciosAsync();
        ViewBag.AlunosSelect = await _db.Alunos
            .AsNoTracking()
            .OrderBy(a => a.NomeCompleto)
            .Select(a => new OpcaoSelect
            {
                Id = a.Id,
                Texto = $"{a.NomeCompleto} — {(string.IsNullOrEmpty(a.Objetivo) ? "sem objetivo" : a.Objetivo)}"
            })
            .ToListAsync();
        ViewBag.PersonaisSelect = await _db.Personais
            .AsNoTracking()
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new OpcaoSelect { Id = p.Id, Texto = p.NomeCompleto })
            .ToListAsync();
        ViewBag.FormArea = "Admin";
        ViewBag.FormController = "Admin";
        ViewBag.CancelarUrl = form.Id > 0
            ? Url.Action(nameof(TreinoDetalhe), new { id = form.Id })
            : Url.Action(nameof(Treinos));
    }

    // ---------------------------------------------------------------------
    // MÓDULOS EM BREVE
    // ---------------------------------------------------------------------

    [HttpGet("modalidades", Name = "AdminModalidades")]
    public IActionResult Modalidades()
    {
        PrepararViewData("Modalidades");
        return View("EmBreve", new EmBreveViewModel
        {
            Recurso = "Modalidades",
            Descricao = "Gerenciar as modalidades e serviços oferecidos pela academia."
        });
    }

    [HttpGet("planos", Name = "AdminPlanos")]
    public IActionResult Planos()
    {
        PrepararViewData("Planos");
        return View("EmBreve", new EmBreveViewModel
        {
            Recurso = "Planos de assinatura",
            Descricao = "Gerenciar planos, preços e benefícios oferecidos aos alunos."
        });
    }

    [HttpGet("conteudo", Name = "AdminConteudo")]
    public IActionResult Conteudo()
    {
        PrepararViewData("Conteúdo do site");
        return View("EmBreve", new EmBreveViewModel
        {
            Recurso = "Conteúdo do site",
            Descricao = "Editar textos, imagens e conteúdos exibidos nas páginas públicas."
        });
    }

    [HttpGet("configuracoes", Name = "AdminConfiguracoes")]
    public IActionResult Configuracoes()
    {
        PrepararViewData("Configurações do site");
        return View(_siteConfig.Obter());
    }

    // ---------------------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------------------

    private static string? LimparTexto(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
