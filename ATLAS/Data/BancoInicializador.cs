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
    /// Garante a tabela de notificações lidas do aluno. Executada sempre que a
    /// aplicação sobe (idempotente): o banco Supabase já existe, então
    /// EnsureCreated não adicionaria tabelas novas — aqui o CREATE IF NOT EXISTS
    /// resolve sem migrations e sem tocar no schema existente.
    /// </summary>
    public static void GarantirNotificacoesLidas(AtlasDbContext db)
    {
        Console.WriteLine("[BancoInicializador] Garantindo tabela NotificacoesLidas...");
        const string sql = """
            CREATE TABLE IF NOT EXISTS "NotificacoesLidas" (
                "Id" serial PRIMARY KEY,
                "AlunoId" integer NOT NULL,
                "Chave" text NOT NULL,
                "VistoEm" timestamptz NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_NotificacoesLidas_AlunoId_Chave"
                ON "NotificacoesLidas" ("AlunoId", "Chave");
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    JOIN pg_class r ON r.oid = c.confrelid
                    WHERE c.contype = 'f'
                      AND t.relname = 'NotificacoesLidas'
                      AND r.relname = 'Usuarios'
                ) THEN
                    ALTER TABLE "NotificacoesLidas"
                        ADD CONSTRAINT "FK_NotificacoesLidas_Usuarios_AlunoId"
                        FOREIGN KEY ("AlunoId") REFERENCES "Usuarios" ("Id") ON DELETE CASCADE;
                END IF;
            END $$;
            """;
        db.Database.ExecuteSqlRaw(sql);
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
