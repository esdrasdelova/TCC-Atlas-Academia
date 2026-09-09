using ATLAS.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.Services;

/// <summary>
/// Health check que valida de fato o banco (conexão + capacidade de leitura).
/// Sem dependências extras — um SELECT trivial basta para o SLI do /healthz.
/// </summary>
public class BancoHealthCheck : IHealthCheck
{
    private readonly AtlasDbContext _db;

    public BancoHealthCheck(AtlasDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT 1";
            await _db.Database.OpenConnectionAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return HealthCheckResult.Healthy("Banco de dados acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Banco de dados inacessível.", ex);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }
}