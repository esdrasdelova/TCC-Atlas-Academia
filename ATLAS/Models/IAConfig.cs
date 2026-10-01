namespace ATLAS.Models;

/// <summary>
/// Configuração do assistente virtual com IA (seção "IA" do appsettings.json e
/// variáveis de ambiente com prefixo IA__, ex.: IA__ApiKey).
///
/// Compatível com a API de "chat completions" da OpenAI (também funciona com
/// provedores compatíveis, como OpenRouter/Groq, ajustando BaseUrl + Modelo).
///
/// A chave NUNCA é exposta ao navegador: as chamadas saem do servidor.
/// </summary>
public class IAConfig
{
    /// <summary>Chave da API (env IA__ApiKey).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>URL base da API (ex.: https://api.openai.com/v1).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Modelo de chat (ex.: gpt-4o-mini).</summary>
    public string Modelo { get; set; } = "gpt-4o-mini";

    /// <summary>Teto de tokens da resposta (controle de custo).</summary>
    public int MaxTokens { get; set; } = 600;

    /// <summary>Criatividade da resposta (0 a 1).</summary>
    public double Temperatura { get; set; } = 0.5;

    /// <summary>Tempo máximo de espera pela resposta da API.</summary>
    public int TempoLimiteSegundos { get; set; } = 30;
}
