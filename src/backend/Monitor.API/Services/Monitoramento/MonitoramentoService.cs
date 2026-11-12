using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs;
using Monitor.API.DTOs.Verificacoes;
using Monitor.API.Models;

namespace Monitor.API.Services.Monitoramento;

public interface IMonitoramentoService
{
    /// <summary>
    /// Verifica um serviço, grava o resultado no histórico e atualiza o status atual.
    /// Retorna null se o serviço não existir.
    /// </summary>
    Task<VerificacaoResponse?> VerificarAsync(int servicoId, CancellationToken ct = default);
}

public class MonitoramentoService(
    MonitorDbContext db,
    IVerificadorHttp verificador,
    ILogger<MonitoramentoService> logger) : IMonitoramentoService
{
    public async Task<VerificacaoResponse?> VerificarAsync(int servicoId, CancellationToken ct = default)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == servicoId, ct);
        if (servico is null) return null;

        var url = servico.ObterUrl();
        var resultado = await verificador.VerificarAsync(url, ct);
        var agora = DateTime.UtcNow;

        var verificacao = new Verificacao
        {
            ServicoId = servico.Id,
            Status = resultado.Status,
            TempoRespostaMs = resultado.TempoRespostaMs,
            CodigoHttp = resultado.CodigoHttp,
            MensagemErro = resultado.MensagemErro,
            VerificadoEm = agora
        };

        if (servico.StatusAtual != resultado.Status)
        {
            // Ponto de extensão para a futura Fase 7 (Alertas): mudança de estado.
            logger.LogInformation(
                "Serviço {Nome} ({Url}) mudou de {Anterior} para {Atual}",
                servico.Nome, url, servico.StatusAtual, resultado.Status);
        }

        servico.StatusAtual = resultado.Status;
        servico.UltimoTempoRespostaMs = resultado.TempoRespostaMs;
        servico.UltimaVerificacaoEm = agora;
        servico.ProximaVerificacaoEm = servico.Ativo ? agora.AddSeconds(servico.IntervaloSegundos) : null;

        db.Verificacoes.Add(verificacao);
        await db.SaveChangesAsync(ct);

        return verificacao.ParaResponse();
    }
}

