namespace ATLAS.Models.ViewModels;

/// <summary>
/// Notificação derivada de dados reais do aluno (treino ativo, avaliação
/// próxima, status da matrícula). Exibida no sino e na página de notificações.
/// </summary>
public class AlunoNotificacaoItem
{
    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    /// <summary>"info" | "alerta" — controla o destaque visual no dropdown.</summary>
    public string Tipo { get; set; } = "info";

    /// <summary>Chave estável da notificação (persistida como "lida" no banco quando visualizada).</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Indica se o aluno já visualizou esta notificação (persistido no Supabase).</summary>
    public bool Lida { get; set; }

    /// <summary>Rota de contexto da notificação (ex.: /aluno/treinos).</summary>
    public string Url { get; set; } = "#";
}

/// <summary>Chaves de notificações marcadas como lidas pelo cliente (fetch JSON).</summary>
public class MarcarNotificacoesLidasDto
{
    public List<string> Chaves { get; set; } = new();
}

/// <summary>
/// Conteúdo do estado vazio reutilizável — sempre com CTA quando aplicável.
/// </summary>
public class EstadoVazioViewModel
{
    public string Icone { get; set; } = "dumbbell";

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string? RotuloCta { get; set; }

    public string? UrlCta { get; set; }
}

/// <summary>Dados da página de avaliação física do aluno.</summary>
public class AlunoAvaliacaoViewModel
{
    public DateTime? ProximaAvaliacao { get; set; }

    public string PersonalNome { get; set; } = "A definir";
}