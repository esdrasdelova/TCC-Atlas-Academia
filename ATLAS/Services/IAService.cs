using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ATLAS.Models;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Integração real com IA via API de chat completions (compatível com OpenAI).
/// A chave vem de configuração do servidor (IA__ApiKey) e nunca vai ao cliente.
/// </summary>
public class IAService : IIAService
{
    // Instrução de sistema: define o papel, o idioma e os limites do Atlas.
    private const string SystemPrompt = """
        Você é o Assistente Atlas, o assistente virtual do Atlas Centro de Treinamento.
        Responda sempre em português brasileiro, de forma educada e objetiva, em respostas CURTAS (no máximo ~120 palavras).

        Você responde SOMENTE sobre: o funcionamento do Atlas (horários, planos, modalidades, cadastro, login, recuperação de senha, áreas do site), musculação, treinos, exercícios físicos, atividade física, hábitos de treino, alimentação no contexto de exercício e uso da área do aluno.

        Se a pessoa perguntar sobre qualquer assunto fora disso (política, notícias, história, programação, receitas, etc.), responda educadamente que só pode ajudar com dúvidas da academia/treinos/uso do site e ofereça ajuda nesse tema. Nunca responda o conteúdo de assuntos fora do escopo.

        Como o Atlas também trabalha com fisioterapia e reabilitação: nunca diagnostique doenças, nunca prescreva tratamentos de reabilitação individualizados e nunca recomende exercícios potencialmente perigosos. Oriente a procurar um profissional qualificado sempre que a situação exigir.

        Não compartilhe dados de outros alunos, senhas, tokens ou informações administrativas. Se não souber algo, diga e sugira falar com a recepção do Atlas.
        """;

    private readonly IAConfig _cfg;
    private readonly HttpClient _http;
    private readonly ILogger<IAService> _logger;

    public IAService(IOptions<IAConfig> cfg, HttpClient http, ILogger<IAService> logger)
    {
        _cfg = cfg.Value;
        _http = http;
        _logger = logger;

        if (_http.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_cfg.TempoLimiteSegundos, 5, 120));
        }
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_cfg.ApiKey);

    public async Task<RespostaIA> ConversarAsync(IReadOnlyList<MensagemIA> historico, string mensagem, CancellationToken ct = default)
    {
        if (!Configurado)
        {
            return new RespostaIA(StatusIA.NaoConfigurado,
                "O assistente virtual ainda não foi configurado neste servidor. Defina a variável de ambiente IA__ApiKey e reinicie a aplicação.");
        }

        var mensagens = new List<object> { new { role = "system", content = SystemPrompt } };
        foreach (var m in historico)
        {
            if (m.Papel is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Conteudo))
            {
                mensagens.Add(new { role = m.Papel, content = m.Conteudo });
            }
        }
        mensagens.Add(new { role = "user", content = mensagem });

        var payload = new
        {
            model = _cfg.Modelo,
            messages = mensagens,
            max_tokens = Math.Clamp(_cfg.MaxTokens, 64, 2000),
            temperature = Math.Clamp(_cfg.Temperatura, 0, 1.5)
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_cfg.BaseUrl.TrimEnd('/')}/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.ApiKey);

            using var response = await _http.SendAsync(request, ct);
            var corpo = await response.Content.ReadAsStringAsync(ct);

            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("IA: limite da API atingido (429).");
                return new RespostaIA(StatusIA.LimiteExcedido, "O assistente está sobrecarregado no momento. Tente novamente em instantes.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IA: API respondeu {Status}. Trecho: {Trecho}", (int)response.StatusCode,
                    corpo.Length > 200 ? corpo[..200] : corpo);
                return new RespostaIA(StatusIA.Indisponivel, "Não consegui falar com o serviço de IA agora. Tente novamente em instantes.");
            }

            using var doc = JsonDocument.Parse(corpo);
            var texto = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(texto))
            {
                return new RespostaIA(StatusIA.Erro, "A IA respondeu sem conteúdo. Pode reformular a pergunta?");
            }

            return new RespostaIA(StatusIA.Sucesso, texto.Trim());
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("IA: tempo limite excedido ao chamar a API.");
            return new RespostaIA(StatusIA.Indisponivel, "A resposta demorou demais para chegar. Tente novamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IA: erro ao chamar a API.");
            return new RespostaIA(StatusIA.Indisponivel, "Não consegui falar com o serviço de IA agora. Tente novamente em instantes.");
        }
    }
}
