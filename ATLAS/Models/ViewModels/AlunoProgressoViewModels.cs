using ATLAS.Models.Entities;

namespace ATLAS.Models.ViewModels;

/// <summary>Dados da página "Medidas e evolução" do aluno.</summary>
public class MedidasViewModel
{
    /// <summary>Medidas em ordem decrescente de data (mais recente primeiro).</summary>
    public List<MedidaCorporal> Historico { get; set; } = new();

    /// <summary>Medição mais recente, usada nos cards de resumo.</summary>
    public MedidaCorporal? Atual => Historico.FirstOrDefault();

    /// <summary>Medição anterior à atual, base para o cálculo da variação.</summary>
    public MedidaCorporal? Anterior => Historico.Skip(1).FirstOrDefault();

    public bool TemDados => Historico.Count > 0;
}

/// <summary>Dados da página "Fotos de progresso" do aluno.</summary>
public class FotosProgressoViewModel
{
    /// <summary>Fotos em ordem decrescente de envio.</summary>
    public List<FotoProgresso> Fotos { get; set; } = new();

    public bool TemDados => Fotos.Count > 0;
}

/// <summary>Resumo de uma foto com a data já formatada para exibição.</summary>
public class FotoProgressoItem
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Legenda { get; set; }
    public DateTime EnviadaEm { get; set; }
}

/// <summary>Dados da página "Pagamentos e faturas" do aluno.</summary>
public class FinanceiroViewModel
{
    /// <summary>Faturas em ordem decrescente de vencimento.</summary>
    public List<Fatura> Faturas { get; set; } = new();

    public bool TemDados => Faturas.Count > 0;

    public decimal TotalPago => Faturas
        .Where(f => f.Status == Enums.StatusFatura.Paga)
        .Sum(f => f.Valor);

    public decimal TotalEmAberto => Faturas
        .Where(f => f.Status != Enums.StatusFatura.Paga)
        .Sum(f => f.Valor);
}

/// <summary>Dados do card de frequência no dashboard do aluno.</summary>
public class FrequenciaResumo
{
    public int TreinosEsteMes { get; set; }

    public int TreinosNaSemana { get; set; }

    /// <summary>Dias consecutivos com treino, terminando hoje ou ontem.</summary>
    public int SequenciaDias { get; set; }

    /// <summary>Últimos 7 dias (mais antigo → hoje) marcando quem teve treino.</summary>
    public List<FrequenciaDia> UltimosDias { get; set; } = new();

    public bool TemDados => TreinosEsteMes > 0;
}

/// <summary>Um dia da faixa de frequência semanal.</summary>
public class FrequenciaDia
{
    public DateTime Data { get; set; }

    public bool Treinou { get; set; }

    /// <summary>Inicial do dia da semana (D, S, T, Q, Q, S, S).</summary>
    public string InicialDia { get; set; } = string.Empty;
}
