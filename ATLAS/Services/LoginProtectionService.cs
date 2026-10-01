using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;

namespace ATLAS.Services;

/// <summary>
/// Implementação em memória do <see cref="ILoginProtectionService"/>.
///
/// Janela deslizante: as falhas dentro da janela configurada contam para o
/// limite. Ao estourar o limite, a chave fica bloqueada por um tempo fixo.
/// O acúmulo é varrido periodicamente para impedir crescimento sem limite.
/// </summary>
public class LoginProtectionService : ILoginProtectionService
{
    private sealed class Registro
    {
        public DateTime? BloqueadoAte { get; set; }
        public readonly Queue<DateTime> Falhas = new();
    }

    private readonly ConcurrentDictionary<string, Registro> _registros = new();
    private readonly int _limiteTentativas;
    private readonly TimeSpan _janela;
    private readonly TimeSpan _bloqueio;
    private readonly ILogger<LoginProtectionService> _logger;

    public LoginProtectionService(IOptions<LoginProtectionOptions> opcoes, ILogger<LoginProtectionService> logger)
    {
        _limiteTentativas = opcoes.Value.LimiteTentativas;
        _janela = opcoes.Value.Janela;
        _bloqueio = opcoes.Value.Bloqueio;
        _logger = logger;
    }

    public string CriarChave(string email, string? ipRemoto)
    {
        var normalizado = (email ?? string.Empty).Trim().ToLowerInvariant();
        var origem = string.IsNullOrWhiteSpace(ipRemoto) ? "desconhecida" : NormalizarIp(ipRemoto);
        return $"{normalizado}|{origem}";
    }

    /// <summary>
    /// Trata todos os loopbacks como a mesma origem: o navegador resolver
    /// "localhost" ora como ::1 ora como 127.0.0.1 não pode dividir a contagem
    /// de tentativas em duas chaves (senão o bloqueio nunca dispara em dev).
    /// </summary>
    private static string NormalizarIp(string ip)
    {
        if (IPAddress.TryParse(ip, out var endereco) && IPAddress.IsLoopback(endereco))
        {
            return "loopback";
        }

        return ip;
    }

    public ResultadoProtecaoLogin PodeTentar(string chave)
    {
        if (!_registros.TryGetValue(chave, out var registro))
        {
            return new ResultadoProtecaoLogin(true, TimeSpan.Zero);
        }

        lock (registro)
        {
            RemoverFalhasExpiradas(registro);

            if (registro.BloqueadoAte.HasValue)
            {
                var restante = registro.BloqueadoAte.Value - DateTime.UtcNow;
                if (restante > TimeSpan.Zero)
                {
                    return new ResultadoProtecaoLogin(false, restante);
                }

                // Bloqueio expirado: libera e zera o contador para recomeçar limpo.
                registro.BloqueadoAte = null;
                registro.Falhas.Clear();
            }

            return new ResultadoProtecaoLogin(true, TimeSpan.Zero);
        }
    }

    public void RegistrarFalha(string chave)
    {
        var registro = _registros.GetOrAdd(chave, _ => new Registro());

        lock (registro)
        {
            RemoverFalhasExpiradas(registro);

            registro.Falhas.Enqueue(DateTime.UtcNow);
            if (registro.Falhas.Count < _limiteTentativas)
            {
                return;
            }

            var bloqueio = DateTime.UtcNow.Add(_bloqueio);
            registro.BloqueadoAte = bloqueio;
            registro.Falhas.Clear();

            _logger.LogWarning(
                "Chave de login temporariamente bloqueada até {BloqueadoAte} ({Janela:g}, limite {Limite}).",
                bloqueio, _janela, _limiteTentativas);
        }

        LimparRegistrosAntigos();
    }

    public void RegistrarSucesso(string chave)
    {
        _registros.TryRemove(chave, out _);
    }

    /// <summary>Remove falhas fora da janela deslizante (chamado dentro do lock).</summary>
    private void RemoverFalhasExpiradas(Registro registro)
    {
        var corte = DateTime.UtcNow.Subtract(_janela);
        while (registro.Falhas.Count > 0 && registro.Falhas.Peek() < corte)
        {
            registro.Falhas.Dequeue();
        }
    }

    /// <summary>Evita crescimento infinito do dicionário após muitos logins.</summary>
    private void LimparRegistrosAntigos()
    {
        if (_registros.Count <= 2_000)
        {
            return;
        }

        var corte = DateTime.UtcNow.Subtract(_janela + _bloqueio);
        foreach (var (chave, registro) in _registros)
        {
            lock (registro)
            {
                // Chave sem bloqueio ativo e sem falhas dentro da janela: pode sumir.
                if (registro.BloqueadoAte == null && registro.Falhas.Count == 0)
                {
                    _registros.TryRemove(chave, out _);
                }
                // Bloqueio já terminou e as falhas expiraram: também pode sumir.
                else if (registro.BloqueadoAte.HasValue &&
                         registro.BloqueadoAte.Value < corte &&
                         (registro.Falhas.Count == 0 || registro.Falhas.Peek() < corte))
                {
                    _registros.TryRemove(chave, out _);
                }
            }
        }
    }
}

/// <summary>Parâmetros da proteção de login (seção "LoginProtection" do appsettings).</summary>
public class LoginProtectionOptions
{
    /// <summary>Máximo de tentativas inválidas dentro da janela.</summary>
    public int LimiteTentativas { get; set; } = 5;

    /// <summary>Janela deslizante em que as tentativas são contadas.</summary>
    public TimeSpan Janela { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Bloqueio aplicado ao estourar o limite.</summary>
    public TimeSpan Bloqueio { get; set; } = TimeSpan.FromMinutes(15);
}