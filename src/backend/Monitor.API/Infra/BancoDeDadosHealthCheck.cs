using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Monitor.API.Data;

namespace Monitor.API.Infra;

/// <summary>
/// Health check do próprio Monitor: verifica se o PostgreSQL está acessível.
/// Exposto em GET /health (útil para Docker, Nginx e para o Monitor monitorar a si mesmo).
/// </summary>
public class BancoDeDadosHealthCheck(MonitorDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL acessível.")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar ao PostgreSQL.", ex);
        }
    }
}

