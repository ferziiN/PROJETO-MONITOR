using Microsoft.Extensions.Diagnostics.HealthChecks;
using Monitor.API.Data;

namespace Monitor.API.Infra;

/// <summary>
/// Health check do próprio Monitor: verifica se o banco de dados configurado (PostgreSQL ou SQLite) está acessível.
/// Exposto em GET /health (útil para Docker, Nginx e para o Monitor monitorar a si mesmo).
/// </summary>
public class BancoDeDadosHealthCheck(MonitorDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = db.Database.ProviderName ?? "Banco de dados";
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy($"Banco de dados acessível ({provider}).")
                : HealthCheckResult.Unhealthy($"Não foi possível conectar ao banco ({provider}).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar ao banco de dados.", ex);
        }
    }
}
