using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main()
    {
        // Connection string lida do appsettings.Secrets.json (fora do Git) ou da
        // variavel de ambiente ATLAS_CONNECTION. Nunca escrever a senha no codigo.
        var connStr = ObterConnectionString();

        Console.WriteLine("Conectando ao Supabase...");
        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            Console.WriteLine("Conectado!");

            // Verificar se colunas ja existem
            await using (var check = new NpgsqlCommand(@"SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='Usuarios' AND column_name IN ('PasswordResetToken','PasswordResetTokenExpires')", conn))
            await using (var reader = await check.ExecuteReaderAsync())
            {
                var found = new System.Collections.Generic.List<string>();
                while (await reader.ReadAsync())
                {
                    found.Add(reader.GetString(0));
                }
                Console.WriteLine("Colunas existentes: " + (found.Count == 0 ? "nenhuma" : string.Join(", ", found)));
            }

            // Adicionar colunas se nao existirem (idempotente)
            await using (var add1 = new NpgsqlCommand(@"ALTER TABLE ""Usuarios"" ADD COLUMN IF NOT EXISTS ""PasswordResetToken"" text NULL", conn))
            {
                var r = await add1.ExecuteNonQueryAsync();
                Console.WriteLine($"ADD PasswordResetToken: {r} row(s) affected");
            }
            await using (var add2 = new NpgsqlCommand(@"ALTER TABLE ""Usuarios"" ADD COLUMN IF NOT EXISTS ""PasswordResetTokenExpires"" timestamp with time zone NULL", conn))
            {
                var r = await add2.ExecuteNonQueryAsync();
                Console.WriteLine($"ADD PasswordResetTokenExpires: {r} row(s) affected");
            }

            // Confirmar
            await using (var verify = new NpgsqlCommand(@"SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_schema='public' AND table_name='Usuarios' AND column_name LIKE 'PasswordReset%'", conn))
            await using (var reader = await verify.ExecuteReaderAsync())
            {
                Console.WriteLine("=== VERIFICACAO FINAL ===");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  {reader.GetString(0)} | {reader.GetString(1)} | nullable={reader.GetString(2)}");
                }
            }

            Console.WriteLine("=== MIGRATION APLICADA COM SUCESSO ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"  inner: {ex.InnerException.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Procura a connection string do Atlas sem nunca fixa-la no codigo:
    /// 1) variavel de ambiente ATLAS_CONNECTION
    /// 2) appsettings.Secrets.json (sobe a partir da pasta do executavel ate encontrar)
    /// </summary>
    static string ObterConnectionString()
    {
        var doAmbiente = Environment.GetEnvironmentVariable("ATLAS_CONNECTION");
        if (!string.IsNullOrWhiteSpace(doAmbiente))
        {
            return doAmbiente;
        }

        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta != null)
        {
            var arquivo = Path.Combine(pasta.FullName, "appsettings.Secrets.json");
            if (File.Exists(arquivo))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(arquivo));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var conexoes)
                    && conexoes.TryGetProperty("Atlas", out var cs))
                {
                    return cs.GetString() ?? string.Empty;
                }
            }

            pasta = pasta.Parent;
        }

        Console.WriteLine("ERRO: connection string nao encontrada. Defina ATLAS_CONNECTION ou crie ATLAS/appsettings.Secrets.json.");
        Environment.Exit(1);
        return string.Empty;
    }
}
