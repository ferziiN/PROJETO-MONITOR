using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs;
using Monitor.API.DTOs.Verificacoes;
using Monitor.API.Models;

namespace Monitor.API.Services.Monitoramento;

public interface IMonitoramentoService
{
    /// <summary>
    /// Verifica um serviço, grava o resultado no histórico, processa alertas de transição
    /// e atualiza o status atual. Retorna null se o serviço não existir.
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

        var statusAnterior = servico.StatusAtual;
        if (statusAnterior != resultado.Status)
        {
            logger.LogInformation(
                "Serviço {Nome} ({Url}) mudou de {Anterior} para {Atual}",
                servico.Nome, url, statusAnterior, resultado.Status);

            await RegistrarAlertaTransicaoAsync(servico, statusAnterior, resultado, agora, ct);
        }

        servico.StatusAtual = resultado.Status;
        servico.UltimoTempoRespostaMs = resultado.TempoRespostaMs;
        servico.UltimaVerificacaoEm = agora;
        servico.ProximaVerificacaoEm = servico.Ativo ? agora.AddSeconds(servico.IntervaloSegundos) : null;

        db.Verificacoes.Add(verificacao);
        await db.SaveChangesAsync(ct);

        return verificacao.ParaResponse();
    }

    private async Task RegistrarAlertaTransicaoAsync(
        Servico servico,
        StatusServico statusAnterior,
        ResultadoVerificacao resultado,
        DateTime agora,
        CancellationToken ct)
    {
        if (resultado.Status == StatusServico.Offline)
        {
            var detalhe = string.IsNullOrWhiteSpace(resultado.MensagemErro)
                ? "Serviço indisponível."
                : resultado.MensagemErro;

            db.Alertas.Add(new Alerta
            {
                ServicoId = servico.Id,
                Tipo = TipoAlerta.Indisponibilidade,
                StatusAnterior = statusAnterior,
                StatusNovo = StatusServico.Offline,
                Mensagem = $"[{servico.Nome}] Ficou OFFLINE — {detalhe}",
                Reconhecido = false,
                CriadoEm = agora
            });
        }
        else if (resultado.Status == StatusServico.Atencao)
        {
            db.Alertas.Add(new Alerta
            {
                ServicoId = servico.Id,
                Tipo = TipoAlerta.Lentidao,
                StatusAnterior = statusAnterior,
                StatusNovo = StatusServico.Atencao,
                Mensagem = $"[{servico.Nome}] Em ATENÇÃO — Resposta elevada ({resultado.TempoRespostaMs} ms).",
                Reconhecido = false,
                CriadoEm = agora
            });
        }
        else if (resultado.Status == StatusServico.Online
                 && (statusAnterior is StatusServico.Offline or StatusServico.Atencao))
        {
            // Resolve automaticamente alertas pendentes deste serviço
            var pendentes = await db.Alertas
                .Where(a => a.ServicoId == servico.Id && a.ResolvidoEm == null)
                .ToListAsync(ct);

            foreach (var pendente in pendentes)
            {
                pendente.ResolvidoEm = agora;
                pendente.Reconhecido = true;
            }

            db.Alertas.Add(new Alerta
            {
                ServicoId = servico.Id,
                Tipo = TipoAlerta.Recuperacao,
                StatusAnterior = statusAnterior,
                StatusNovo = StatusServico.Online,
                Mensagem = $"[{servico.Nome}] Recuperado — Voltou a responder ({resultado.TempoRespostaMs} ms).",
                Reconhecido = true,
                CriadoEm = agora,
                ResolvidoEm = agora
            });
        }
    }
}
