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
        try
        {
            var sql = """
                CREATE TABLE IF NOT EXISTS `NotificacoesLidas` (
                    `Id` int AUTO_INCREMENT PRIMARY KEY,
                    `AlunoId` int NOT NULL,
                    `Chave` longtext NOT NULL,
                    `VistoEm` datetime(6) NOT NULL,
                    UNIQUE KEY `IX_NotificacoesLidas_AlunoId_Chave` (`AlunoId`, `Chave`(191)),
                    CONSTRAINT `FK_NotificacoesLidas_Usuarios_AlunoId` FOREIGN KEY (`AlunoId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
                );
                """;
            db.Database.ExecuteSqlRaw(sql);
        }
        catch
        {
            try
            {
                const string sqlPg = """
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
                db.Database.ExecuteSqlRaw(sqlPg);
            }
            catch { }
        }
    }

    /// <summary>
    /// Garante as tabelas dos módulos do aluno (medidas, fotos de progresso,
    /// faturas e frequência). Idempotente: o banco já existe, então
    /// EnsureCreated não adicionaria tabelas novas — o CREATE IF NOT EXISTS
    /// resolve sem migrations e sem tocar no schema existente.
    /// </summary>
    public static void GarantirTabelasAluno(AtlasDbContext db)
    {
        Console.WriteLine("[BancoInicializador] Garantindo tabelas do aluno (medidas, fotos, faturas, frequencia)...");

        var sqlMedidas = """
            CREATE TABLE IF NOT EXISTS `Medidas` (
                `Id` int AUTO_INCREMENT PRIMARY KEY,
                `AlunoId` int NOT NULL,
                `DataMedicao` datetime(6) NOT NULL,
                `PesoKg` decimal(6,2) NULL,
                `AlturaCm` decimal(6,2) NULL,
                `PeitoCm` decimal(6,2) NULL,
                `CinturaCm` decimal(6,2) NULL,
                `QuadrilCm` decimal(6,2) NULL,
                `BracoCm` decimal(6,2) NULL,
                `CoxaCm` decimal(6,2) NULL,
                `PanturrilhaCm` decimal(6,2) NULL,
                `PercentualGordura` decimal(5,2) NULL,
                `Observacoes` longtext NULL,
                INDEX `IX_Medidas_AlunoId_DataMedicao` (`AlunoId`, `DataMedicao`),
                CONSTRAINT `FK_Medidas_Usuarios_AlunoId` FOREIGN KEY (`AlunoId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
            );
            """;

        var sqlFotos = """
            CREATE TABLE IF NOT EXISTS `Fotos` (
                `Id` int AUTO_INCREMENT PRIMARY KEY,
                `AlunoId` int NOT NULL,
                `Url` longtext NOT NULL,
                `Legenda` longtext NULL,
                `EnviadaEm` datetime(6) NOT NULL,
                INDEX `IX_Fotos_AlunoId_EnviadaEm` (`AlunoId`, `EnviadaEm`),
                CONSTRAINT `FK_Fotos_Usuarios_AlunoId` FOREIGN KEY (`AlunoId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
            );
            """;

        var sqlFaturas = """
            CREATE TABLE IF NOT EXISTS `Faturas` (
                `Id` int AUTO_INCREMENT PRIMARY KEY,
                `AlunoId` int NOT NULL,
                `Descricao` longtext NOT NULL,
                `Valor` decimal(10,2) NOT NULL,
                `DataVencimento` datetime(6) NOT NULL,
                `DataPagamento` datetime(6) NULL,
                `Status` int NOT NULL,
                INDEX `IX_Faturas_AlunoId_DataVencimento` (`AlunoId`, `DataVencimento`),
                CONSTRAINT `FK_Faturas_Usuarios_AlunoId` FOREIGN KEY (`AlunoId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
            );
            """;

        var sqlFrequencia = """
            CREATE TABLE IF NOT EXISTS `Frequencias` (
                `Id` int AUTO_INCREMENT PRIMARY KEY,
                `AlunoId` int NOT NULL,
                `DataTreino` datetime(6) NOT NULL,
                `TreinoNome` longtext NULL,
                INDEX `IX_Frequencias_AlunoId_DataTreino` (`AlunoId`, `DataTreino`),
                CONSTRAINT `FK_Frequencias_Usuarios_AlunoId` FOREIGN KEY (`AlunoId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
            );
            """;

        foreach (var sql in new[] { sqlMedidas, sqlFotos, sqlFaturas, sqlFrequencia })
        {
            try
            {
                db.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[BancoInicializador] Aviso ao criar tabela do aluno: " + ex.Message);
            }
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

            // MySQL/MariaDB: o "schema" é o próprio banco de dados (DATABASE()).
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @nome";
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
