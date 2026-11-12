using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs;
using Monitor.API.DTOs.Dashboard;
using Monitor.API.Models;

namespace Monitor.API.Services;

public interface IDashboardService
{
    Task<DashboardResponse> ObterResumoAsync(CancellationToken ct = default);
}

/// <summary>
/// Monta a visão geral do dashboard (RF04).
/// </summary>
public class DashboardService(MonitorDbContext db) : IDashboardService
{
    public async Task<DashboardResponse> ObterResumoAsync(CancellationToken ct = default)
    {
        var servicos = await db.Servicos.AsNoTracking()
            .OrderBy(s => s.Nome)
            .ToListAsync(ct);

        // Contadores de status consideram apenas os serviços com monitoramento ativo.
        var ativos = servicos.Where(s => s.Ativo).ToList();

        var temposOnline = ativos
            .Where(s => (s.StatusAtual is StatusServico.Online or StatusServico.Atencao) && s.UltimoTempoRespostaMs.HasValue)
            .Select(s => s.UltimoTempoRespostaMs!.Value)
            .ToList();

        return new DashboardResponse(
            TotalServicos: servicos.Count,
            Ativos: ativos.Count,
            Inativos: servicos.Count - ativos.Count,
            Online: ativos.Count(s => s.StatusAtual == StatusServico.Online),
            Offline: ativos.Count(s => s.StatusAtual == StatusServico.Offline),
            Atencao: ativos.Count(s => s.StatusAtual == StatusServico.Atencao),
            Desconhecido: ativos.Count(s => s.StatusAtual == StatusServico.Desconhecido),
            TempoMedioRespostaMs: temposOnline.Count == 0 ? null : Math.Round(temposOnline.Average(), 1),
            GeradoEm: DateTime.UtcNow,
            Servicos: servicos.Select(s => s.ParaResponse()).ToList());
    }
}
