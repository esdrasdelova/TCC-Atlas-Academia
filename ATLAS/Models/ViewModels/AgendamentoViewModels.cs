using System.ComponentModel.DataAnnotations;
using ATLAS.Models.Enums;

namespace ATLAS.Models.ViewModels;

/// <summary>Projeção segura de um agendamento para APIs/JSON.</summary>
public class AgendamentoDto
{
    public int Id { get; set; }

    /// <summary>Ex.: "Avaliação física", "Aula experimental", "Sessão de pilates".</summary>
    public string Tipo { get; set; } = string.Empty;

    public DateTime DataHora { get; set; }

    public StatusAgendamento Status { get; set; }

    public int AlunoId { get; set; }
    public string AlunoNome { get; set; } = string.Empty;

    public int? PersonalId { get; set; }
    public string? PersonalNome { get; set; }
}

public static class AgendamentoDtoMapeamento
{
    public static AgendamentoDto ParaDto(Entities.Agendamento a) => new()
    {
        Id = a.Id,
        Tipo = a.Tipo,
        DataHora = a.DataHora,
        Status = a.Status,
        AlunoId = a.AlunoId,
        AlunoNome = a.Aluno?.NomeCompleto ?? string.Empty,
        PersonalId = a.PersonalId,
        PersonalNome = a.Personal?.NomeCompleto
    };
}

/// <summary>Corpo do POST /api/agendamentos.</summary>
public class CriarAgendamentoRequest
{
    [Required(ErrorMessage = "Informe o tipo de agendamento.")]
    [StringLength(80, ErrorMessage = "O tipo deve ter no máximo 80 caracteres.")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe data e horário.")]
    public DateTime DataHora { get; set; }

    /// <summary>Personal preferido (opcional).</summary>
    public int? PersonalId { get; set; }
}

/// <summary>Corpo do PATCH /api/agendamentos/{id}/status.</summary>
public class AtualizarStatusRequest
{
    [Required(ErrorMessage = "Informe o novo status.")]
    [EnumDataType(typeof(StatusAgendamento), ErrorMessage = "Status inválido.")]
    public StatusAgendamento Status { get; set; }
}