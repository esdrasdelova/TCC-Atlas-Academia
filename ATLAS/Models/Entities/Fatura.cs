using ATLAS.Models.Enums;

namespace ATLAS.Models.Entities;

/// <summary>
/// Fatura da mensalidade do aluno. Cadastrada pelo aluno (perfil de
/// demonstração) e exibida em "Pagamentos e faturas".
/// </summary>
public class Fatura
{
    public int Id { get; set; }

    public int AlunoId { get; set; }
    public Aluno Aluno { get; set; } = null!;

    /// <summary>Descrição da cobrança (ex.: "Mensalidade — Plano Anual").</summary>
    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateTime DataVencimento { get; set; }

    /// <summary>Data do pagamento; nula enquanto a fatura estiver em aberto.</summary>
    public DateTime? DataPagamento { get; set; }

    public StatusFatura Status { get; set; } = StatusFatura.Pendente;
}
