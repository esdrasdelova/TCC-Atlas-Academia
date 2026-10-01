using System.Security.Claims;
using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.ViewModels;
using ATLAS.Models.Enums;
using ATLAS.Models.Entities;
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
    private readonly ISiteConfigService _siteConfig;

    public AlunoController(ITreinoService treinos, AtlasDbContext db, ISiteConfigService siteConfig)
    {
        _treinos = treinos;
        _db = db;
        _siteConfig = siteConfig;
    }

    /// <summary>Id e nome vêm dos claims de autenticação.</summary>
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Prepara dados comuns das páginas da área: título do topo, usuário,
    /// sidebar e as notificações derivadas exibidas no sino.
    /// </summary>
    private async Task PrepararViewDataAsync(string titulo)
    {
        ViewData["Titulo"] = titulo;
        ViewData["UsuarioNome"] = User.Identity?.Name ?? "Aluno";
        ViewData["UsuarioPapel"] = Permissoes.Aluno;
        ViewData["Sidebar"] = "_SidebarAluno";
        ViewData["MostrarSino"] = true;
        ViewData["Notificacoes"] = await ConstruirNotificacoesAsync(UsuarioId);
    }

    /// <summary>Notificações honestas, derivadas de dados reais do aluno.</summary>
    private async Task<List<AlunoNotificacaoItem>> ConstruirNotificacoesAsync(int alunoId)
    {
        var itens = new List<AlunoNotificacaoItem>();

        var aluno = await _db.Alunos
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alunoId);

        if (aluno == null)
        {
            return itens;
        }

        var treinoAtivo = await _db.Treinos
            .AsNoTracking()
            .Where(t => t.AlunoId == alunoId && t.Ativo && t.Publicado)
            .OrderByDescending(t => t.DataCriacao)
            .FirstOrDefaultAsync();

        if (aluno.Status != StatusConta.Ativa)
        {
            itens.Add(new AlunoNotificacaoItem
            {
                Titulo = "Matrícula inativa",
                Descricao = "Regularize sua matrícula para continuar treinando sem interrupções.",
                Tipo = "alerta",
                Chave = "matricula",
                Url = "/aluno/financeiro"
            });
        }

        if (aluno.ProximaAvaliacao.HasValue)
        {
            var dias = (aluno.ProximaAvaliacao.Value.Date - DateTime.Today).Days;
            itens.Add(new AlunoNotificacaoItem
            {
                Titulo = dias <= 7 ? "Avaliação física próxima" : "Avaliação física agendada",
                Descricao = dias <= 7
                    ? $"Sua avaliação está marcada para {aluno.ProximaAvaliacao.Value:dd/MM/yyyy}."
                    : $"Avaliação marcada para {aluno.ProximaAvaliacao.Value:dd/MM/yyyy}.",
                Tipo = dias <= 7 ? "alerta" : "info",
                Chave = $"avaliacao:{aluno.ProximaAvaliacao.Value:yyyy-MM-dd}",
                Url = "/aluno/avaliacao"
            });
        }

        if (treinoAtivo != null)
        {
            itens.Add(new AlunoNotificacaoItem
            {
                Titulo = "Seu treino está disponível",
                Descricao = $"A ficha \"{treinoAtivo.Nome}\" atualizada está pronta para consulta.",
                Tipo = "info",
                Chave = $"treino:{treinoAtivo.Id}",
                Url = "/aluno/treinos"
            });
        }

        if (string.IsNullOrWhiteSpace(aluno.Objetivo))
        {
            itens.Add(new AlunoNotificacaoItem
            {
                Titulo = "Defina seu objetivo",
                Descricao = "Informe seu objetivo no perfil para o personal montar sua estratégia.",
                Tipo = "info",
                Chave = "objetivo",
                Url = "/aluno/perfil"
            });
        }

        // Calcula o que já foi visualizado (persistido no banco) para marcar o badge.
        if (itens.Count > 0)
        {
            var chavesVistas = await _db.NotificacoesLidas
                .AsNoTracking()
                .Where(l => l.AlunoId == alunoId)
                .Select(l => l.Chave)
                .ToListAsync();

            var vistas = new HashSet<string>(chavesVistas, StringComparer.Ordinal);
            foreach (var item in itens)
            {
                item.Lida = vistas.Contains(item.Chave);
            }
        }

        return itens;
    }

    /// <summary>
    /// Persiste as chaves como lidas pelo aluno (idempotente). Foi usada na
    /// abertura do painel do sino e ao visitar a página de notificações.
    /// </summary>
    private async Task MarcarChavesLidasAsync(int alunoId, List<string> chaves)
    {
        var limpas = chaves
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (limpas.Count == 0)
        {
            return;
        }

        var jaVistas = await _db.NotificacoesLidas
            .Where(l => l.AlunoId == alunoId)
            .Select(l => l.Chave)
            .ToListAsync();

        var conjunto = new HashSet<string>(jaVistas, StringComparer.Ordinal);
        foreach (var chave in limpas)
        {
            if (conjunto.Add(chave))
            {
                _db.NotificacoesLidas.Add(new NotificacaoLida
                {
                    AlunoId = alunoId,
                    Chave = chave,
                    VistoEm = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Área do Aluno";
        await PrepararViewDataAsync("Área do Aluno");

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
            PersonalRegistro = aluno.Personal?.RegistroProfissional,
            PersonalTelefone = aluno.Personal?.Telefone,
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
        await PrepararViewDataAsync("Meus Treinos");

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
        await PrepararViewDataAsync(treino.Nome);
        return View(treino);
    }

    [HttpGet("historico")]
    public async Task<IActionResult> Historico()
    {
        ViewData["Title"] = "Histórico de treinos";
        await PrepararViewDataAsync("Histórico de treinos");

        var lista = await _treinos.ListarTreinosDoAlunoAsync(UsuarioId);
        return View(lista);
    }

    [HttpGet("exercicios")]
    public async Task<IActionResult> Exercicios()
    {
        ViewData["Title"] = "Biblioteca de exercícios";
        await PrepararViewDataAsync("Biblioteca de exercícios");

        var catalogo = await _db.Exercicios
            .AsNoTracking()
            .Where(e => e.Ativo)
            .OrderBy(e => e.GrupoMuscular).ThenBy(e => e.Nome)
            .ToListAsync();

        return View(catalogo);
    }

    [HttpGet("aulas")]
    public async Task<IActionResult> Aulas()
    {
        ViewData["Title"] = "Aulas coletivas";
        await PrepararViewDataAsync("Aulas coletivas");

        return View(_siteConfig.Obter().Modalidades);
    }

    [HttpGet("avaliacao")]
    public async Task<IActionResult> Avaliacao()
    {
        ViewData["Title"] = "Avaliação física";
        await PrepararViewDataAsync("Avaliação física");

        var aluno = await CarregarAlunoAsync();
        var modelo = new AlunoAvaliacaoViewModel
        {
            ProximaAvaliacao = aluno?.ProximaAvaliacao,
            PersonalNome = aluno?.Personal?.NomeCompleto ?? "A definir"
        };

        return View(modelo);
    }

    [HttpGet("medidas")]
    public async Task<IActionResult> Medidas()
    {
        ViewData["Title"] = "Medidas e evolução";
        await PrepararViewDataAsync("Medidas e evolução");
        return View();
    }

    [HttpGet("fotos-progresso")]
    public async Task<IActionResult> FotosProgresso()
    {
        ViewData["Title"] = "Fotos de progresso";
        await PrepararViewDataAsync("Fotos de progresso");
        return View();
    }

    [HttpGet("financeiro")]
    public async Task<IActionResult> Financeiro()
    {
        ViewData["Title"] = "Pagamentos e faturas";
        await PrepararViewDataAsync("Pagamentos e faturas");
        return View();
    }

    [HttpGet("notificacoes")]
    public async Task<IActionResult> Notificacoes()
    {
        ViewData["Title"] = "Notificações";
        var itens = await ConstruirNotificacoesAsync(UsuarioId);

        // Visitar a página conta como leitura: marca tudo e o badge zera.
        await MarcarChavesLidasAsync(UsuarioId, itens.Select(i => i.Chave).ToList());
        foreach (var item in itens)
        {
            item.Lida = true;
        }

        ViewData["Titulo"] = "Notificações";
        ViewData["UsuarioNome"] = User.Identity?.Name ?? "Aluno";
        ViewData["UsuarioPapel"] = Permissoes.Aluno;
        ViewData["Sidebar"] = "_SidebarAluno";
        ViewData["MostrarSino"] = true;
        ViewData["Notificacoes"] = itens;

        return View(itens);
    }

    /// <summary>Marca as notificações atuais como lidas (chamado ao abrir o sino).</summary>
    [HttpPost("notificacoes/lidas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarNotificacoesLidas([FromBody] MarcarNotificacoesLidasDto? dto)
    {
        await MarcarChavesLidasAsync(UsuarioId, dto?.Chaves ?? new List<string>());
        return NoContent();
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        ViewData["Title"] = "Meu Perfil";
        await PrepararViewDataAsync("Meu Perfil");

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