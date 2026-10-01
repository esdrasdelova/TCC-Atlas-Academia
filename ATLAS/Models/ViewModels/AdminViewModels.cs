using System.ComponentModel.DataAnnotations;

namespace ATLAS.Models.ViewModels;

/// <summary>
/// ViewModels da área administrativa (dashboard, gestão de alunos e personais).
/// O binding usa apenas estas classes — as entidades do banco nunca recebem
/// dados direto do POST (proteção contra overposting).
/// </summary>

public class AdminDashboardViewModel
{
    public int TotalAlunos { get; set; }
    public int AlunosAtivos { get; set; }
    public int AlunosInativos { get; set; }
    public int TotalPersonais { get; set; }
    public int TotalAdministradores { get; set; }

    /// <summary>Treinos personalizados publicados.</summary>
    public int TreinosPersonalizados { get; set; }

    public List<CadastroResumo> CadastrosRecentes { get; set; } = new();
}

public class CadastroResumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Papel { get; set; } = string.Empty;
    public DateTime Data { get; set; }
}

public class OpcaoSelect
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// ALUNOS
// ─────────────────────────────────────────────────────────────────────────────

public class AlunoListItemViewModel
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string? Objetivo { get; set; }
    public string? PersonalNome { get; set; }
    public bool ContaAtiva { get; set; }
    public DateTime CriadoEm { get; set; }
    public int QtdTreinos { get; set; }
    public string? NomeTreinoAtivo { get; set; }
}

public class AlunoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo.")]
    [MinLength(3, ErrorMessage = "O nome deve ter pelo menos 3 caracteres.")]
    [StringLength(120)]
    [Display(Name = "Nome completo")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(160)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data de nascimento")]
    public DateTime DataNascimento { get; set; }

    [StringLength(200)]
    [Display(Name = "Objetivo")]
    public string? Objetivo { get; set; }

    [Display(Name = "Personal responsável")]
    public int? PersonalId { get; set; }

    /// <summary>Obrigatório no cadastro; na edição use "Nova senha" para redefinir.</summary>
    [Display(Name = "Senha")]
    public string? Senha { get; set; }

    /// <summary>Preencher somente para trocar a senha de uma conta existente.</summary>
    [Display(Name = "Nova senha")]
    public string? NovaSenha { get; set; }

    [Display(Name = "Conta ativa")]
    public bool ContaAtiva { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────────────────────
// PERSONAIS / PROFESSORES
// ─────────────────────────────────────────────────────────────────────────────

public class PersonalListItemViewModel
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string? RegistroProfissional { get; set; }
    public string? Especialidade { get; set; }
    public bool ContaAtiva { get; set; }
    public DateTime CriadoEm { get; set; }
    public int QtdAlunos { get; set; }
    public int QtdTreinos { get; set; }
}

public class PersonalFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo.")]
    [MinLength(3, ErrorMessage = "O nome deve ter pelo menos 3 caracteres.")]
    [StringLength(120)]
    [Display(Name = "Nome completo")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(160)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefone")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data de nascimento")]
    public DateTime DataNascimento { get; set; }

    [StringLength(30)]
    [Display(Name = "Registro profissional (CREF)")]
    public string? RegistroProfissional { get; set; }

    [StringLength(120)]
    [Display(Name = "Especialidade")]
    public string? Especialidade { get; set; }

    [Display(Name = "Senha")]
    public string? Senha { get; set; }

    [Display(Name = "Nova senha")]
    public string? NovaSenha { get; set; }

    [Display(Name = "Conta ativa")]
    public bool ContaAtiva { get; set; } = true;
}