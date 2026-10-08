using ATLAS.Models.Enums;

namespace ATLAS.Models.Entities;

/// <summary>
/// Base de todos os usuários do sistema (aluno, personal e administrador).
/// Pronta para mapeamento com Entity Framework (tabela única TPH ou por hierarquia).
/// </summary>
public abstract class Usuario
{
    public int Id { get; set; }

    public string NomeCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;

    /// <summary>Senha armazenada sempre como hash — nunca em texto puro.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    public DateTime DataNascimento { get; set; }

    public StatusConta Status { get; set; } = StatusConta.Ativa;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
}
