namespace ATLAS.Middleware;

/// <summary>
/// Aplica cabeçalhos de segurança básicos em toda resposta e impede o cache
/// de páginas autenticadas (áreas /aluno, /personal, /admin e a API), que
/// não devem ser armazenadas no navegador ou em proxies intermediários.
/// </summary>
public class SegurancaHttpMiddleware
{
    // Áreas que exigem login: nenhuma dessas deve ser cacheada.
    private static readonly string[] CaminhosSensiveis =
    [
        "/aluno",
        "/personal",
        "/admin",
        "/api/",
        "/login",
        "/cadastro",
        "/logout",
        "/esqueci-senha",
        "/redefinir-senha",
        "/sem-acesso"
    ];

    private readonly RequestDelegate _proximo;

    public SegurancaHttpMiddleware(RequestDelegate proximo)
    {
        _proximo = proximo;
    }

    public Task Invoke(HttpContext contexto)
    {
        var resposta = contexto.Response;

        resposta.Headers["X-Content-Type-Options"] = "nosniff";
        resposta.Headers["X-Frame-Options"] = "SAMEORIGIN";
        resposta.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        resposta.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Permissions-Policy desligada para recursos que o site não usa.
        resposta.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=(), payment=()";

        if (CaminhoEhSensivel(contexto.Request.Path))
        {
            resposta.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            resposta.Headers["Pragma"] = "no-cache";
        }

        return _proximo(contexto);
    }

    private static bool CaminhoEhSensivel(string caminho)
    {
        var path = caminho.AsSpan();
        foreach (var prefixo in CaminhosSensiveis)
        {
            if (path.Equals(prefixo, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}