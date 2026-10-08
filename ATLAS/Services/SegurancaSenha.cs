using System.Security.Cryptography;

namespace ATLAS.Services;

/// <summary>
/// Hash de senha com PBKDF2 (nativo do .NET, sem dependências externas).
/// Formato armazenado: pbkdf2-sha256$iterações$salt$hash — tudo em Base64.
/// </summary>
public static class SegurancaSenha
{
    private const int IteracoesPadrao = 100_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    public static string GerarHash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, IteracoesPadrao, HashAlgorithmName.SHA256, TamanhoHash);

        return $"pbkdf2-sha256${IteracoesPadrao}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string? hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(hashArmazenado))
        {
            return false;
        }

        var partes = hashArmazenado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
        {
            return false;
        }

        byte[] salt, esperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>Indica se o hash está em formato válido gerado por este serviço.</summary>
    public static bool EhHashValido(string? hash) =>
        !string.IsNullOrWhiteSpace(hash) &&
        hash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal) &&
        hash.Split('$').Length == 4;
}
