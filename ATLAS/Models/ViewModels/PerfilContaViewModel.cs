namespace ATLAS.Models.ViewModels;

/// <summary>
/// Perfil da própria conta nas áreas do ADM e do personal — os dados que o
/// usuário logado consulta e edita (nome e telefone). E-mail, nascimento e
/// registro profissional são somente leitura, seguindo o perfil do aluno.
/// </summary>
public class PerfilContaViewModel
{
    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;

    public DateTime? DataNascimento { get; set; }

    /// <summary>Mês/ano de criação da conta, já formatado para exibição.</summary>
    public string MembroDesde { get; set; } = string.Empty;

    public bool ContaAtiva { get; set; }

    /// <summary>Tipo de conta exibido no perfil (ex.: "Administrador").</summary>
    public string Papel { get; set; } = string.Empty;

    /// <summary>Preenchido apenas no perfil do personal (CREF).</summary>
    public string? RegistroProfissional { get; set; }

    /// <summary>Preenchido apenas no perfil do personal.</summary>
    public string? Especialidade { get; set; }
}
