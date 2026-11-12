using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Monitor.API.Data;

namespace Monitor.API.Services.Monitoramento;

/// <summary>
/// Agendador em segundo plano (RNF01): a cada "IntervaloVarreduraSegundos" busca os
/// serviços ativos cuja próxima verificação já venceu e os verifica em paralelo,
/// respeitando o limite de concorrência. Também aplica a retenção do histórico.
/// </summary>
public class MonitoramentoWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoramentoOptions> options,
    ILogger<MonitoramentoWorker> logger) : BackgroundService
{
    private readonly MonitoramentoOptions _opcoes = options.Value;
    private DateTime _ultimaLimpeza = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opcoes.Habilitado)
        {
            logger.LogWarning("Monitoramento automático desabilitado (Monitoramento:Habilitado = false).");
            return;
        }

        logger.LogInformation(
            "Monitoramento iniciado. Varredura a cada {Intervalo}s, timeout {Timeout}s, até {Max} verificações simultâneas.",
            _opcoes.IntervaloVarreduraSegundos, _opcoes.TimeoutSegundos, _opcoes.MaxVerificacoesSimultaneas);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _opcoes.IntervaloVarreduraSegundos)));

        do
        {
            try
            {
                await ExecutarCicloAsync(stoppingToken);
                await LimparHistoricoAntigoAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Uma falha (ex.: banco fora do ar) não pode derrubar o agendador.
                logger.LogError(ex, "Erro no ciclo de monitoramento.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExecutarCicloAsync(CancellationToken ct)
    {
        List<int> idsVencidos;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();
            var agora = DateTime.UtcNow;

            idsVencidos = await db.Servicos
                .AsNoTracking()
                .Where(s => s.Ativo && (s.ProximaVerificacaoEm == null || s.ProximaVerificacaoEm <= agora))
                .OrderBy(s => s.ProximaVerificacaoEm)
                .Select(s => s.Id)
                .ToListAsync(ct);
        }

        if (idsVencidos.Count == 0) return;

        logger.LogDebug("Verificando {Quantidade} serviço(s).", idsVencidos.Count);

        var paralelismo = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, _opcoes.MaxVerificacoesSimultaneas),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(idsVencidos, paralelismo, async (id, token) =>
        {
            try
            {
                // Um escopo (e DbContext) por verificação: DbContext não é thread-safe.
                await using var scope = scopeFactory.CreateAsyncScope();
                var monitoramento = scope.ServiceProvider.GetRequiredService<IMonitoramentoService>();
                await monitoramento.VerificarAsync(id, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao verificar o serviço {ServicoId}.", id);
            }
        });
    }

    private async Task LimparHistoricoAntigoAsync(CancellationToken ct)
    {
        if (_opcoes.RetencaoHistoricoDias <= 0) return;
        if (DateTime.UtcNow - _ultimaLimpeza < TimeSpan.FromHours(1)) return;

        _ultimaLimpeza = DateTime.UtcNow;
        var limite = DateTime.UtcNow.AddDays(-_opcoes.RetencaoHistoricoDias);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitorDbContext>();

        var removidos = await db.Verificacoes
            .Where(v => v.VerificadoEm < limite)
            .ExecuteDeleteAsync(ct);

        if (removidos > 0)
            logger.LogInformation("Retenção: {Quantidade} verificação(ões) antiga(s) removida(s).", removidos);
    }
}

