using System.Security.Claims;
using ATLAS.Data;
using ATLAS.Models;
using ATLAS.Models.Entities;
using ATLAS.Models.ViewModels;
using ATLAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Areas.Api.Controllers;

/// <summary>
/// API JSON de agendamentos (avaliações, aulas experimentais e sessões).
/// Autenticada por cookie e autorizada por papel — os escopos seguem as
/// mesmas regras das áreas MVC (aluno = só o que é dele; personal = o que é
/// dele ou dos alunos da carteira; adm = irrestrito).
///
/// Exemplos:
///   GET   /api/agendamentos
///   POST  /api/agendamentos                    { "tipo": "...", "dataHora": "...", "personalId": 2 }
///   PATCH /api/agendamentos/{id}/status        { "status": 2 }
/// </summary>
[ApiController]
[Area("Api")]
[Route("api/agendamentos")]
[Authorize]
public class AgendamentosApiController : ControllerBase
{
    private readonly IAgendamentoService _agendamentos;
    private readonly AtlasDbContext _db;

    public AgendamentosApiController(IAgendamentoService agendamentos, AtlasDbContext db)
    {
        _agendamentos = agendamentos;
        _db = db;
    }

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgendamentoDto>>> Listar()
    {
        List<Agendamento> lista;

        if (User.IsInRole(Permissoes.Administrador))
        {
            lista = await _agendamentos.ListarTodosAsync();
        }
        else if (User.IsInRole(Permissoes.Personal))
        {
            lista = await _agendamentos.ListarDoPersonalAsync(UsuarioId);
        }
        else
        {
            lista = await _agendamentos.ListarDoAlunoAsync(UsuarioId);
        }

        return Ok(lista.Select(AgendamentoDtoMapeamento.ParaDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AgendamentoDto>> Obter(int id)
    {
        var agendamento = await _agendamentos.ObterAsync(id);
        if (agendamento == null)
        {
            return NotFound(new { mensagem = "Agendamento não encontrado." });
        }

        if (!await PodeAcessarAsync(agendamento))
        {
            return Forbid();
        }

        return Ok(AgendamentoDtoMapeamento.ParaDto(agendamento));
    }

    [HttpPost]
    public async Task<ActionResult<AgendamentoDto>> Criar(CriarAgendamentoRequest request)
    {
        if (User.IsInRole(Permissoes.Personal) || User.IsInRole(Permissoes.Administrador))
        {
            return Forbid();
        }

        if (request.DataHora <= DateTime.UtcNow.AddMinutes(5))
        {
            return BadRequest(new { mensagem = "A data/hora precisa estar no futuro (mínimo 5 minutos).", campo = nameof(request.DataHora) });
        }

        int? personalId = null;
        if (request.PersonalId.HasValue)
        {
            var personalExiste = await _db.Personais.AnyAsync(p => p.Id == request.PersonalId.Value && p.Status == Models.Enums.StatusConta.Ativa);
            if (!personalExiste)
            {
                return BadRequest(new { mensagem = "Personal selecionado não existe ou está inativo.", campo = nameof(request.PersonalId) });
            }

            personalId = request.PersonalId.Value;
        }

        var id = await _agendamentos.CriarAsync(UsuarioId, request.Tipo!, request.DataHora, personalId);
        var criado = await _agendamentos.ObterAsync(id);
        return CreatedAtAction(nameof(Obter), new { id }, AgendamentoDtoMapeamento.ParaDto(criado!));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> AtualizarStatus(int id, AtualizarStatusRequest request)
    {
        bool admin = User.IsInRole(Permissoes.Administrador);
        bool personal = User.IsInRole(Permissoes.Personal);
        int? alunoId = admin || personal ? null : UsuarioId;
        int? personalId = personal ? UsuarioId : null;

        var resultado = await _agendamentos.AtualizarStatusAsync(id, request.Status, alunoId, personalId, admin);

        return resultado switch
        {
            AtualizacaoAgendamentoResultado.Ok => Ok(new { mensagem = "Status atualizado." }),
            AtualizacaoAgendamentoResultado.NaoEncontrado => NotFound(new { mensagem = "Agendamento não encontrado." }),
            AtualizacaoAgendamentoResultado.SemPermissao => Forbid(),
            AtualizacaoAgendamentoResultado.TransicaoInvalida => Conflict(new
            {
                mensagem = "Transição de status não permitida (ex.: Pendente→Concluído ou alterar algo já cancelado/concluído)."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private async Task<bool> PodeAcessarAsync(Agendamento agendamento)
    {
        if (User.IsInRole(Permissoes.Administrador))
        {
            return true;
        }

        if (User.IsInRole(Permissoes.Personal))
        {
            return agendamento.PersonalId == UsuarioId ||
                   await _db.Alunos.AnyAsync(a => a.Id == agendamento.AlunoId && a.PersonalId == UsuarioId);
        }

        return agendamento.AlunoId == UsuarioId;
    }
}