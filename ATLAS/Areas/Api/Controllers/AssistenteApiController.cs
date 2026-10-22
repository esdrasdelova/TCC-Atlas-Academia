using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ATLAS.Services;

namespace ATLAS.Areas.Api.Controllers;

/// <summary>
/// Endpoint público do Assistente Atlas (conversa livre com IA real).
/// POST /api/assistente/chat  { "mensagem": "...", "historico": [{"papel":"user","texto":"..."}, ...] }
///
/// Proteções: rate limit por IP, limite de tamanho da mensagem e do histórico,
/// sem acesso a banco/dados de usuários e chave da IA somente no servidor.
/// </summary>
[ApiController]
[Area("Api")]
[Route("api/assistente")]
[AllowAnonymous]
public class AssistenteApiController : ControllerBase
{
    private const int MaxMensagem = 1000;
    private const int MaxHistorico = 10;
    private const int MaxJanelaMinutos = 10;
    private const int MaxPorJanela = 20;

    private readonly IIAService _ia;
    private readonly IMemoryCache _cache;

    public AssistenteApiController(IIAService ia, IMemoryCache cache)
    {
        _ia = ia;
        _cache = cache;
    }

    public class HistoricoItem
    {
        public string Papel { get; set; } = "";
        public string Texto { get; set; } = "";
    }

    public class ChatRequest
    {
        public string Mensagem { get; set; } = "";
        public List<HistoricoItem>? Historico { get; set; }
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(ChatRequest? request, CancellationToken ct)
    {
        // Rate limit simples por IP: no máximo 20 mensagens a cada 10 minutos.
        var chave = $"ia:chat:{HttpContext.Connection.RemoteIpAddress}";
        var janela = _cache.GetOrCreate(chave, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(MaxJanelaMinutos);
            return new List<DateTime>();
        })!;
        janela.RemoveAll(d => d < DateTime.UtcNow.AddMinutes(-MaxJanelaMinutos));
        if (janela.Count >= MaxPorJanela)
        {
            return StatusCode(429, new { erro = "Muitas mensagens seguidas. Aguarde alguns minutos e tente de novo." });
        }
        janela.Add(DateTime.UtcNow);

        var mensagem = (request?.Mensagem ?? "").Trim();
        if (string.IsNullOrEmpty(mensagem))
        {
            return BadRequest(new { erro = "Digite uma mensagem para enviar." });
        }
        if (mensagem.Length > MaxMensagem)
        {
            return BadRequest(new { erro = $"A mensagem é muito longa (máximo de {MaxMensagem} caracteres)." });
        }

        // Histórico enxuto e sanitizado: só os últimos itens, truncados.
        var historico = (request?.Historico ?? new List<HistoricoItem>())
            .TakeLast(MaxHistorico)
            .Where(h => h.Papel is "user" or "assistant")
            .Select(h =>
            {
                var texto = h.Texto ?? "";
                return new MensagemIA(h.Papel, texto.Length > MaxMensagem ? texto[..MaxMensagem] : texto);
            })
            .ToList();

        var resposta = await _ia.ConversarAsync(historico, mensagem, ct);

        if (!resposta.Sucesso)
        {
            var status = resposta.Status switch
            {
                StatusIA.NaoConfigurado => 503,
                StatusIA.LimiteExcedido => 429,
                _ => 502
            };
            return StatusCode(status, new { erro = resposta.Texto });
        }

        return Ok(new { resposta = resposta.Texto });
    }
}
