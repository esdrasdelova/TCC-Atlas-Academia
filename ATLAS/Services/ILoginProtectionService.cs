namespace ATLAS.Services;

/// <summary>Resultado da checagem de permissão para tentar o login.</summary>
public readonly record struct ResultadoProtecaoLogin(bool Permitido, TimeSpan TempoRestante);

/// <summary>
/// Proteção contra força bruta no login: limita tentativas por chave
/// (habitualmente e-mail + IP) em uma janela deslizante e impõe um bloqueio
/// temporário quando o limite é estourado. Em memória e lock por chave —
/// suficiente para a escala atual; permite trocar por Redis depois sem
/// mudar a interface.
/// </summary>
public interface ILoginProtectionService
{
    /// <summary>Chave identificadora da tentativa (e-mail normalizado + IP).</summary>
    string CriarChave(string email, string? ipRemoto);

    /// <summary>Consulta se a chave ainda pode tentar o login.</summary>
    ResultadoProtecaoLogin PodeTentar(string chave);

    /// <summary>Registra uma tentativa inválida (credencial errada) para a chave.</summary>
    void RegistrarFalha(string chave);

    /// <summary>Limpa o histórico de falhas após login bem-sucedido.</summary>
    void RegistrarSucesso(string chave);
}