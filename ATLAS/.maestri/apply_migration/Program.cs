using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main()
    {
        // Connection string do user-secrets (extraida para teste)
        var connStr = "Host=aws-0-sa-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.dzqchyvdqeeqvdqeeqetjzfyku;Password=M6a-P37XEYJDD!b;Pooling=true;Minimum Pool Size=1;Maximum Pool Size=2;Timeout=15;Command Timeout=30;Keepalive=30;Ssl Mode=Require;Trust Server Certificate=true";

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
}
