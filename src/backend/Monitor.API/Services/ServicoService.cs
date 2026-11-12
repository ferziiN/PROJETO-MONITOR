using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs;
using Monitor.API.DTOs.Servicos;
using Monitor.API.DTOs.Verificacoes;
using Monitor.API.Models;

namespace Monitor.API.Services;

public interface IServicoService
{
    Task<IReadOnlyList<ServicoResponse>> ListarAsync(bool? ativo, StatusServico? status, CancellationToken ct = default);
    Task<ServicoResponse?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<ServicoResponse> CriarAsync(CriarServicoRequest request, CancellationToken ct = default);
    Task<ServicoResponse?> AtualizarAsync(int id, AtualizarServicoRequest request, CancellationToken ct = default);
    Task<ServicoResponse?> AlterarMonitoramentoAsync(int id, bool ativo, CancellationToken ct = default);
    Task<bool> RemoverAsync(int id, CancellationToken ct = default);

    Task<StatusServicoResponse?> ObterStatusAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VerificacaoResponse>?> ListarVerificacoesAsync(int id, int limite, DateTime? desde, CancellationToken ct = default);
    Task<MetricasServicoResponse?> ObterMetricasAsync(int id, TimeSpan periodo, string rotuloPeriodo, CancellationToken ct = default);
}

/// <summary>
/// Regras de negócio do cadastro de serviços (RF01) e consultas de status/histórico (RF05).
/// </summary>
public class ServicoService(MonitorDbContext db) : IServicoService
{
    public async Task<IReadOnlyList<ServicoResponse>> ListarAsync(
        bool? ativo, StatusServico? status, CancellationToken ct = default)
    {
        var query = db.Servicos.AsNoTracking();

        if (ativo.HasValue) query = query.Where(s => s.Ativo == ativo.Value);
        if (status.HasValue) query = query.Where(s => s.StatusAtual == status.Value);

        var servicos = await query.OrderBy(s => s.Nome).ToListAsync(ct);
        return servicos.Select(s => s.ParaResponse()).ToList();
    }

    public async Task<ServicoResponse?> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var servico = await db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        return servico?.ParaResponse();
    }

    public async Task<ServicoResponse> CriarAsync(CriarServicoRequest request, CancellationToken ct = default)
    {
        var servico = request.ParaEntidade();

        // Agenda a primeira verificação para o próximo ciclo do agendador.
        servico.ProximaVerificacaoEm = servico.Ativo ? DateTime.UtcNow : null;

        db.Servicos.Add(servico);
        await db.SaveChangesAsync(ct);

        return servico.ParaResponse();
    }

    public async Task<ServicoResponse?> AtualizarAsync(int id, AtualizarServicoRequest request, CancellationToken ct = default)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (servico is null) return null;

        var urlAnterior = servico.ObterUrl();
        request.AplicarEm(servico);

        // Se o alvo mudou, o status antigo não vale mais.
        if (servico.ObterUrl() != urlAnterior)
        {
            servico.StatusAtual = StatusServico.Desconhecido;
            servico.UltimoTempoRespostaMs = null;
        }

        servico.ProximaVerificacaoEm = servico.Ativo ? DateTime.UtcNow : null;

        await db.SaveChangesAsync(ct);
        return servico.ParaResponse();
    }

    public async Task<ServicoResponse?> AlterarMonitoramentoAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (servico is null) return null;

        servico.Ativo = ativo;
        servico.ProximaVerificacaoEm = ativo ? DateTime.UtcNow : null;

        await db.SaveChangesAsync(ct);
        return servico.ParaResponse();
    }

    public async Task<bool> RemoverAsync(int id, CancellationToken ct = default)
    {
        // As verificações são removidas em cascata (ver ServicoConfiguration).
        var removidos = await db.Servicos.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
        return removidos > 0;
    }

    public async Task<StatusServicoResponse?> ObterStatusAsync(int id, CancellationToken ct = default)
    {
        var servico = await db.Servicos.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (servico is null) return null;

        var ultima = await db.Verificacoes.AsNoTracking()
            .Where(v => v.ServicoId == id)
            .OrderByDescending(v => v.VerificadoEm)
            .FirstOrDefaultAsync(ct);

        return new StatusServicoResponse(
            servico.Id,
            servico.Nome,
            servico.ObterUrl().ToString(),
            servico.Ativo,
            servico.StatusAtual,
            servico.UltimoTempoRespostaMs,
            ultima?.CodigoHttp,
            servico.UltimaVerificacaoEm,
            ultima?.MensagemErro);
    }

    public async Task<IReadOnlyList<VerificacaoResponse>?> ListarVerificacoesAsync(
        int id, int limite, DateTime? desde, CancellationToken ct = default)
    {
        if (!await db.Servicos.AnyAsync(s => s.Id == id, ct)) return null;

        var query = db.Verificacoes.AsNoTracking().Where(v => v.ServicoId == id);

        if (desde.HasValue)
        {
            var desdeUtc = desde.Value.ToUniversalTime();
            query = query.Where(v => v.VerificadoEm >= desdeUtc);
        }

        var verificacoes = await query
            .OrderByDescending(v => v.VerificadoEm)
            .Take(Math.Clamp(limite, 1, 1000))
            .ToListAsync(ct);

        return verificacoes.Select(v => v.ParaResponse()).ToList();
    }

    public async Task<MetricasServicoResponse?> ObterMetricasAsync(
        int id, TimeSpan periodo, string rotuloPeriodo, CancellationToken ct = default)
    {
        if (!await db.Servicos.AnyAsync(s => s.Id == id, ct)) return null;

        var fim = DateTime.UtcNow;
        var inicio = fim - periodo;

        // Uma única consulta agregada por status.
        var grupos = await db.Verificacoes.AsNoTracking()
            .Where(v => v.ServicoId == id && v.VerificadoEm >= inicio)
            .GroupBy(v => v.Status)
            .Select(g => new
            {
                Status = g.Key,
                Total = g.Count(),
                ComTempo = g.Count(v => v.TempoRespostaMs != null),
                SomaTempo = g.Sum(v => (long?)v.TempoRespostaMs),
                Minimo = g.Min(v => v.TempoRespostaMs),
                Maximo = g.Max(v => v.TempoRespostaMs)
            })
            .ToListAsync(ct);

        int Contar(StatusServico s) => grupos.Where(g => g.Status == s).Sum(g => g.Total);

        var total = grupos.Sum(g => g.Total);
        var online = Contar(StatusServico.Online);
        var offline = Contar(StatusServico.Offline);
        var atencao = Contar(StatusServico.Atencao);

        var comTempo = grupos.Sum(g => g.ComTempo);
        var somaTempo = grupos.Sum(g => g.SomaTempo ?? 0);

        // "Atenção" = respondeu (mais lento), portanto conta como disponível.
        double? disponibilidade = total == 0 ? null : Math.Round((online + atencao) * 100.0 / total, 2);
        double? tempoMedio = comTempo == 0 ? null : Math.Round((double)somaTempo / comTempo, 1);

        return new MetricasServicoResponse(
            id, rotuloPeriodo, inicio, fim, total, online, offline, atencao,
            disponibilidade, tempoMedio,
            grupos.Min(g => g.Minimo),
            grupos.Max(g => g.Maximo));
    }

    /// <summary>
    /// Converte "30m", "24h", "7d" em TimeSpan. Retorna false se o formato for inválido.
    /// </summary>
    public static bool TentarConverterPeriodo(string? texto, out TimeSpan periodo)
    {
        periodo = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        var match = Regex.Match(texto.Trim().ToLowerInvariant(), @"^(\d{1,4})([mhd])$");
        if (!match.Success) return false;

        var valor = int.Parse(match.Groups[1].Value);
        periodo = match.Groups[2].Value switch
        {
            "m" => TimeSpan.FromMinutes(valor),
            "h" => TimeSpan.FromHours(valor),
            _ => TimeSpan.FromDays(valor)
        };

        return periodo > TimeSpan.Zero && periodo <= TimeSpan.FromDays(90);
    }
}

