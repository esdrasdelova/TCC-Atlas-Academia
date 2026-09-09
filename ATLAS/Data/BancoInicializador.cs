using Microsoft.EntityFrameworkCore;

namespace ATLAS.Data;

/// <summary>
/// Garante que o schema do banco exista de forma idempotente no PostgreSQL
/// (Supabase). A verificação usa information_schema (padrão ANSI, suportado
/// pelo Postgres). Em outros providers a consulta também funciona via fallback
/// ao INFORMATION_SCHEMA.
/// </summary>
public static class BancoInicializador
{
    /// <summary>Tabela obrigatória para o app funcionar (raiz da hierarquia TPH).</summary>
    private const string TabelaBase = "Usuarios";

    public static void GarantirSchema(AtlasDbContext db)
    {
        Console.WriteLine("[BancoInicializador] Verificando existencia da tabela " + TabelaBase + "...");
        var existe = TabelaExiste(db, TabelaBase);
        Console.WriteLine("[BancoInicializador] Tabela " + TabelaBase + " existe? " + existe);
        if (existe)
        {
            return;
        }

        Console.WriteLine("[BancoInicializador] Criando schema com EnsureCreated()...");
        try
        {
            db.Database.EnsureCreated();
            var apos = TabelaExiste(db, TabelaBase);
            Console.WriteLine("[BancoInicializador] Apos EnsureCreated: tabela existe? " + apos);
            if (!apos)
            {
                Console.WriteLine("[BancoInicializador] EnsureCreated nao criou a tabela. Forcando via Migrate()...");
                db.Database.Migrate();
                var apos2 = TabelaExiste(db, TabelaBase);
                Console.WriteLine("[BancoInicializador] Apos Migrate: tabela existe? " + apos2);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[BancoInicializador] Erro: " + ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Verifica se a tabela-base existe no banco atual. Um banco ainda não
    /// criado leva a "não existe", permitindo que o EnsureCreated() faça a
    /// criação do schema.
    /// </summary>
    private static bool TabelaExiste(AtlasDbContext db, string nome)
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                db.Database.OpenConnection();
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'public' AND TABLE_NAME = @nome";
            var p = cmd.CreateParameter();
            p.ParameterName = "@nome";
            p.Value = nome;
            cmd.Parameters.Add(p);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }
}
