using Microsoft.EntityFrameworkCore;
using Monitor.API.Models;

namespace Monitor.API.Data;

/// <summary>
/// Contexto do Entity Framework Core com o PostgreSQL.
/// </summary>
public class MonitorDbContext(DbContextOptions<MonitorDbContext> options) : DbContext(options)
{
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Verificacao> Verificacoes => Set<Verificacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as classes IEntityTypeConfiguration<T> de Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MonitorDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PreencherDatasDeAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Preenche automaticamente CriadoEm/AtualizadoEm dos serviços.
    /// </summary>
    private void PreencherDatasDeAuditoria()
    {
        var agora = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Servico>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
                entry.Entity.AtualizadoEm = agora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = agora;
            }
        }
    }
}

