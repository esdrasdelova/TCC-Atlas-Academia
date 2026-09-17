namespace ATLAS.Models.Entities;

/// <summary>
/// Registro de notificação lida por um aluno. As notificações são derivadas
/// de dados reais (treino, avaliação, matrícula, objetivo) e cada uma tem uma
/// chave estável — ver aqui significa "o aluno já visualizou a notificação
/// com esta chave", o que zera o badge do sino de forma persistente.
/// </summary>
public class NotificacaoLida
{
    public int Id { get; set; }

    public int AlunoId { get; set; }

    /// <summary>Chave estável da notificação derivada (ex.: "avaliacao:2026-09-20").</summary>
    public string Chave { get; set; } = string.Empty;

    public DateTime VistoEm { get; set; } = DateTime.UtcNow;

    public Aluno? Aluno { get; set; }
}