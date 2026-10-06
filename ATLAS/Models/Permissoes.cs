namespace ATLAS.Models;

/// <summary>
/// Nomes de papéis usados no controle de acesso.
/// Quando a autenticação for implementada (cookies/JWT + [Authorize]),
/// aplicar aqui: [Authorize(Roles = Permissoes.Administrador)] etc.
/// </summary>
public static class Permissoes
{
    public const string Aluno = "Aluno";
    public const string Personal = "Personal";
    public const string Administrador = "Administrador";
}
