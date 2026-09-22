using System.Security.Claims;
using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.Entities;
using ATLAS.Models.Enums;
using ATLAS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using EntidadeAluno = ATLAS.Models.Entities.Aluno;

namespace ATLAS.Areas.Account.Controllers;

/// <summary>
/// Login, logout e cadastro de alunos com autenticação por cookies.
/// O papel da sessão vem do tipo da conta no banco (Aluno/Personal/Administrador).
/// </summary>
[Area("Account")]
public class AccountController : Controller
{
    private readonly AtlasDbContext _db;
    private readonly ILoginProtectionService _loginProtection;
    private readonly IEmailService _email;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AtlasDbContext db, ILoginProtectionService loginProtection, IEmailService email, IMemoryCache cache, ILogger<AccountController> logger)
    {
        _db = db;
        _loginProtection = loginProtection;
        _email = email;
        _cache = cache;
        _logger = logger;
    }

    // Recuperação de senha por link: limites para não abusar do canal de e-mail.
    private static readonly TimeSpan IntervaloEntreSolicitacoes = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan JanelaSolicitacoes = TimeSpan.FromMinutes(10);
    private const int MaxSolicitacoesNaJanela = 5;
    private static readonly TimeSpan ValidadeTokenRecuperacao = TimeSpan.FromMinutes(30);

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirecionarPorPapel();
        }

        ViewData["Title"] = "Entrar";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string senha, string? returnUrl)
    {
        ViewData["Title"] = "Entrar";

        email = (email ?? string.Empty).Trim().ToLower();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
        {
            ModelState.AddModelError(string.Empty, "Informe e-mail e senha.");
            return View();
        }

        // Proteção contra força bruta: antes de consultar o banco, verifica o
        // limite de tentativas para a chave (e-mail + IP do cliente).
        var chave = _loginProtection.CriarChave(email, HttpContext.Connection.RemoteIpAddress?.ToString());
        var situacao = _loginProtection.PodeTentar(chave);

        if (!situacao.Permitido)
        {
            _logger.LogWarning(
                "Tentativa de login bloqueada para {Email} — aguardar {Tempo}.",
                email, situacao.TempoRestante);

            var minutos = Math.Ceiling(situacao.TempoRestante.TotalMinutes);
            ModelState.AddModelError(string.Empty,
                $"Muitas tentativas de login. Tente novamente em {minutos} min.");
            return View();
        }

        Usuario? usuario;
        try
        {
            usuario = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar login de {Email}.", email);
            ModelState.AddModelError(string.Empty, "O serviço está temporariamente indisponível. Tente novamente em instantes.");
            return View();
        }

        if (usuario == null || !SegurancaSenha.Verificar(senha, usuario.SenhaHash))
        {
            _loginProtection.RegistrarFalha(chave);
            _logger.LogWarning("Falha de login para {Email} (credenciais inválidas).", email);
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View();
        }

        if (usuario.Status != StatusConta.Ativa)
        {
            _logger.LogWarning("Tentativa de login em conta desativada: {Email}.", email);
            ModelState.AddModelError(string.Empty, "Esta conta está desativada. Fale com a administração da academia.");
            return View();
        }

        _loginProtection.RegistrarSucesso(chave);
        _logger.LogInformation("Login bem-sucedido: usuário {Id} ({Email}) — {Papel}.",
            usuario.Id, usuario.Email, usuario switch
            {
                Administrador => Permissoes.Administrador,
                PersonalTrainer => Permissoes.Personal,
                _ => Permissoes.Aluno
            });

        await EntrarAsync(usuario);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirecionarPorPapel();
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("Logout do usuário {Email}.", User.Identity.Name);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("Atlas.Auth", new CookieOptions { Path = "/" });
        TempData["Aviso"] = "Sessão encerrada. Até logo!";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/cadastro")]
    public IActionResult Cadastro()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirecionarPorPapel();
        }

        ViewData["Title"] = "Criar conta";
        return View();
    }

    [HttpPost("/cadastro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastro(string nomeCompleto, string email, string telefone, DateTime dataNascimento, string senha)
    {
        ViewData["Title"] = "Criar conta";

        nomeCompleto = (nomeCompleto ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        telefone = (telefone ?? string.Empty).Trim();

        if (nomeCompleto.Length < 3)
        {
            ModelState.AddModelError(string.Empty, "Informe seu nome completo.");
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(string.Empty, "Informe um e-mail válido.");
        }
        else
        {
            try
            {
                if (await EmailEmUsoAsync(email))
                {
                    ModelState.AddModelError(string.Empty, "Já existe uma conta com este e-mail.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar e-mail duplicado no cadastro.");
                ModelState.AddModelError(string.Empty, "Não foi possível consultar o cadastro agora. Tente novamente em instantes.");
            }
        }

        if (senha is null || senha.Length < 6)
        {
            ModelState.AddModelError(string.Empty, "A senha deve ter pelo menos 6 caracteres.");
        }

        if (dataNascimento == default || dataNascimento > DateTime.UtcNow.AddYears(-10) || dataNascimento < new DateTime(1920, 1, 1))
        {
            ModelState.AddModelError(string.Empty, "Informe uma data de nascimento válida (mínimo 10 anos).");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var aluno = new EntidadeAluno
        {
            NomeCompleto = nomeCompleto,
            Email = email,
            Telefone = telefone,
            DataNascimento = dataNascimento,
            SenhaHash = SegurancaSenha.GerarHash(senha!),
            Status = StatusConta.Ativa,
            CriadoEm = DateTime.UtcNow
        };

        try
        {
            _db.Alunos.Add(aluno);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EhViolacaoDeChaveUnica(ex))
        {
            // Corrida entre dois cadastros com o mesmo e-mail: o índice único pega.
            _logger.LogWarning("Tentativa de cadastro com e-mail duplicado: {Email}", email);
            ModelState.AddModelError(string.Empty, "Já existe uma conta com este e-mail.");
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar conta para {Email}", email);
            ModelState.AddModelError(string.Empty, "Não foi possível criar a conta agora. Tente novamente em alguns instantes.");
            return View();
        }

        try
        {
            await EntrarAsync(aluno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sessão não foi criada após cadastro de {Email}", email);
            TempData["Sucesso"] = "Conta criada com sucesso! Faça login para continuar.";
            return RedirectToAction(nameof(Login));
        }

        TempData["Sucesso"] = $"Conta criada com sucesso! Bem-vindo(a), {aluno.NomeCompleto.Split(' ')[0]}.";
        return RedirectToAction("Index", "Aluno", new { area = "Aluno" });
    }

    /// <summary>Tela exibida quando o usuário logado tenta acessar área sem permissão.</summary>
    [HttpGet("/sem-acesso")]
    public IActionResult SemAcesso()
    {
        ViewData["Title"] = "Acesso restrito";
        return View();
    }

    [HttpGet("/esqueci-senha")]
    public IActionResult EsqueciSenha()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirecionarPorPapel();
        }

        ViewData["Title"] = "Recuperar senha";
        return View();
    }

    [HttpPost("/esqueci-senha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EsqueciSenha(string email)
    {
        ViewData["Title"] = "Recuperar senha";
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            TempData["Aviso"] = "Se o e-mail estiver cadastrado, enviaremos o link de redefinição.";
            return RedirectToAction(nameof(EsqueciSenha));
        }

        // Limita a frequência de solicitações por e-mail e IP para não esgotar a cota de envio.
        var chaveCliente = $"recuperacao:req:{email}|{HttpContext.Connection.RemoteIpAddress ?? null}";
        if (_cache.TryGetValue(chaveCliente, out DateTime _))
        {
            TempData["Aviso"] = "Aguarde um instante antes de solicitar outro link.";
            return RedirectToAction(nameof(EsqueciSenha));
        }

        // Janela deslizante: máximo de solicitações em curto período (anti-spam).
        var chaveJanela = $"recuperacao:janela:{email}|{HttpContext.Connection.RemoteIpAddress ?? null}";
        var historico = _cache.GetOrCreate(chaveJanela, _ => new List<DateTime>());
        if (historico is not null)
        {
            var corte = DateTime.UtcNow.Subtract(JanelaSolicitacoes);
            historico.RemoveAll(d => d < corte);
            if (historico.Count >= MaxSolicitacoesNaJanela)
            {
                TempData["Aviso"] = "Muitas solicitações. Tente novamente mais tarde.";
                return RedirectToAction(nameof(EsqueciSenha));
            }
        }

        Usuario? usuario;
        try
        {
            usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar {Email} na recuperação de senha.", email);
            TempData["Aviso"] = "O serviço está temporariamente indisponível. Tente novamente em instantes.";
            return RedirectToAction(nameof(EsqueciSenha));
        }

        if (usuario != null)
        {
            // Novo link a cada solicitação — invalida o token anterior. No banco
            // fica apenas o hash SHA-256; o token em texto puro só viaja no
            // e-mail, nunca é persistido nem logado.
            var token = GerarTokenRecuperacao();
            usuario.PasswordResetToken = GerarHashToken(token);
            usuario.PasswordResetTokenExpires = DateTime.UtcNow.Add(ValidadeTokenRecuperacao);

            var link = $"{Request.Scheme}://{Request.Host}{Url.Content("~/redefinir-senha")}?token={Uri.EscapeDataString(token)}";

            bool enviado;
            try
            {
                await _db.SaveChangesAsync();
                enviado = await _email.EnviarLinkRecuperacaoAsync(email, link, usuario.NomeCompleto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar ou enviar link de recuperação para {Email}", email);
                enviado = false;
            }

            if (!enviado)
            {
                TempData["Aviso"] = "Não foi possível enviar o e-mail agora. Tente novamente em alguns instantes.";
                return RedirectToAction(nameof(EsqueciSenha));
            }
        }

        // Só registra a solicitação quando o fluxo segue (não bloqueia a recuperação após falha interna).
        _cache.Set(chaveCliente, DateTime.UtcNow, IntervaloEntreSolicitacoes);
        if (historico is not null)
        {
            historico.Add(DateTime.UtcNow);
            _cache.Set(chaveJanela, historico, JanelaSolicitacoes);
        }

        // Mensagem única e genérica — não revela se a conta existe.
        TempData["Aviso"] = "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.";
        return RedirectToAction(nameof(EsqueciSenha));
    }

    /// <summary>
    /// Etapa final do fluxo por link: o token opaco chega na query string do
    /// e-mail. A conta é localizada somente pelo hash do token — não existe
    /// campo de e-mail no formulário, então é impossível mirar outra conta.
    /// </summary>
    [HttpGet("/redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha(string? token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirecionarPorPapel();
        }

        ViewData["Title"] = "Redefinir senha";
        ViewBag.Token = token ?? string.Empty;
        ViewBag.TokenValido = await TokenRecuperacaoValidoAsync(token);

        return View();
    }

    [HttpPost("/redefinir-senha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedefinirSenha(string token, string novaSenha, string confirmarSenha)
    {
        ViewData["Title"] = "Redefinir senha";
        token = (token ?? string.Empty).Trim();
        ViewBag.Token = token;

        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
            ModelState.AddModelError(string.Empty, "A nova senha deve ter pelo menos 6 caracteres.");
        if (novaSenha != confirmarSenha)
            ModelState.AddModelError(string.Empty, "As senhas não conferem.");

        if (!ModelState.IsValid)
        {
            ViewBag.TokenValido = await TokenRecuperacaoValidoAsync(token);
            return View();
        }

        Usuario? usuario;
        try
        {
            var hash = GerarHashToken(token);
            usuario = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.PasswordResetToken == hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar link de redefinição de senha.");
            ModelState.AddModelError(string.Empty, "O serviço está temporariamente indisponível. Tente novamente.");
            ViewBag.TokenValido = false;
            return View();
        }

        if (usuario == null || usuario.PasswordResetTokenExpires == null || usuario.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            // Link inexistente ou expirado: mensagem única, sem dizer qual.
            if (usuario != null)
            {
                InutilizarToken(usuario);
                try { await _db.SaveChangesAsync(); }
                catch (Exception ex) { _logger.LogError(ex, "Não foi possível invalidar token expirado do usuário {Id}.", usuario.Id); }
            }

            _logger.LogInformation("Tentativa de redefinição com link inválido ou expirado.");
            ViewBag.Token = string.Empty;
            ViewBag.TokenValido = false;
            return View();
        }

        usuario.SenhaHash = SegurancaSenha.GerarHash(novaSenha);
        InutilizarToken(usuario); // uso único: o link deixa de valer na hora.

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar nova senha do usuário {Id}.", usuario.Id);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar a nova senha. Tente novamente.");
            ViewBag.TokenValido = true;
            return View();
        }

        _logger.LogInformation("Senha redefinida por link para {Email}.", usuario.Email);
        TempData["Sucesso"] = "Senha alterada com sucesso! Faça login com a nova senha.";
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Confere se existe token válido (presente e não expirado) para exibir o formulário.</summary>
    private async Task<bool> TokenRecuperacaoValidoAsync(string? token)
    {
        token = (token ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(token)) return false;

        try
        {
            var hash = GerarHashToken(token);
            return await _db.Usuarios.AnyAsync(u =>
                u.PasswordResetToken == hash &&
                u.PasswordResetTokenExpires != null &&
                u.PasswordResetTokenExpires > DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar link de recuperação.");
            return false;
        }
    }

    private async Task EntrarAsync(Usuario usuario)
    {
        var papel = usuario switch
        {
            Administrador => Permissoes.Administrador,
            PersonalTrainer => Permissoes.Personal,
            _ => Permissoes.Aluno
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NomeCompleto),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, papel)
        };

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identidade);

        HttpContext.User = principal;
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    /// <summary>Envia cada perfil para a sua área inicial.</summary>
    private IActionResult RedirecionarPorPapel()
    {
        if (User.IsInRole(Permissoes.Administrador))
        {
            return RedirectToAction("Index", "Admin", new { area = "Admin" });
        }

        if (User.IsInRole(Permissoes.Personal))
        {
            return RedirectToAction("Index", "Personal", new { area = "Personal" });
        }

        return RedirectToAction("Index", "Aluno", new { area = "Aluno" });
    }

    private Task<bool> EmailEmUsoAsync(string email) =>
        _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    /// <summary>Token opaco de 256 bits (CSPRG) para o link de recuperação — Base64Url, seguro em query string.</summary>
    private static string GerarTokenRecuperacao()
    {
        Span<byte> bytes = stackalloc byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>Hash SHA-256 do token — é isto que fica armazenado no banco.</summary>
    private static string GerarHashToken(string token)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private static void InutilizarToken(Usuario usuario)
    {
        usuario.PasswordResetToken = null;
        usuario.PasswordResetTokenExpires = null;
    }

    private static bool EhViolacaoDeChaveUnica(DbUpdateException ex)
    {
        // PostgreSQL: SQLSTATE 23505 — unique_violation.
        if (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return true;
        }
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
