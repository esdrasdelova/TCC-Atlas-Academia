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
    private readonly ILogger<AccountController> _logger;

    public AccountController(AtlasDbContext db, ILoginProtectionService loginProtection, IEmailService email, ILogger<AccountController> logger)
    {
        _db = db;
        _loginProtection = loginProtection;
        _email = email;
        _logger = logger;
    }

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

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);

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
        email = (email ?? string.Empty).Trim();
        telefone = (telefone ?? string.Empty).Trim();

        if (nomeCompleto.Length < 3)
        {
            ModelState.AddModelError(string.Empty, "Informe seu nome completo.");
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(string.Empty, "Informe um e-mail válido.");
        }
        else if (await EmailEmUsoAsync(email))
        {
            ModelState.AddModelError(string.Empty, "Já existe uma conta com este e-mail.");
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

        _db.Alunos.Add(aluno);
        await _db.SaveChangesAsync();

        await EntrarAsync(aluno);
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
        ViewData["Title"] = "Recuperar senha";
        return View();
    }

    [HttpPost("/esqueci-senha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EsqueciSenha(string email)
    {
        ViewData["Title"] = "Recuperar senha";
        email = (email ?? string.Empty).Trim().ToLower();

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            TempData["Aviso"] = "Se o e-mail estiver cadastrado, enviaremos o codigo.";
            return RedirectToAction(nameof(RedefinirSenha), new { email });
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario != null)
        {
            var rng = new Random();
            var codigo = rng.Next(100000, 999999).ToString();
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codigo));
            var hash = Convert.ToHexString(bytes).ToLowerInvariant();
            usuario.PasswordResetToken = hash;
            usuario.PasswordResetTokenExpires = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Codigo de recuperacao gerado para {Email}: {Codigo}", email, codigo);

            try
            {
                await _email.EnviarCodigoRecuperacaoAsync(email, codigo, usuario.NomeCompleto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no envio do email de recuperacao para {Email}", email);
            }
        }

        TempData["Aviso"] = "Se o e-mail estiver cadastrado, enviaremos o codigo de 6 digitos.";
        return RedirectToAction(nameof(RedefinirSenha), new { email });
    }

    [HttpGet("/redefinir-senha")]
    public IActionResult RedefinirSenha(string? email)
    {
        ViewData["Title"] = "Redefinir senha";
        ViewBag.Email = email ?? string.Empty;
        return View();
    }

    [HttpPost("/redefinir-senha")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedefinirSenha(string email, string codigo, string novaSenha, string confirmarSenha)
    {
        ViewData["Title"] = "Redefinir senha";
        ViewBag.Email = email;

        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 6)
            ModelState.AddModelError(string.Empty, "A senha deve ter pelo menos 6 caracteres.");
        if (novaSenha != confirmarSenha)
            ModelState.AddModelError(string.Empty, "A confirmacao nao confere.");

        if (!ModelState.IsValid) return View();

        email = (email ?? string.Empty).Trim().ToLower();
        using var sha = System.Security.Cryptography.SHA256.Create();
        var codigoHash = Convert.ToHexString(
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes((codigo ?? string.Empty).Trim()))
        ).ToLowerInvariant();

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario == null
            || usuario.PasswordResetToken != codigoHash
            || usuario.PasswordResetTokenExpires == null
            || usuario.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "Codigo invalido ou expirado.");
            return View();
        }

        usuario.SenhaHash = SegurancaSenha.GerarHash(novaSenha);
        usuario.PasswordResetToken = null;
        usuario.PasswordResetTokenExpires = null;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Senha redefinida para {Email}.", email);
        await EntrarAsync(usuario);
        return RedirecionarPorPapel();
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
}
