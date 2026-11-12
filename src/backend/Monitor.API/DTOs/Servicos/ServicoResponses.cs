using Monitor.API.Models;

namespace Monitor.API.DTOs.Servicos;

public record ServicoResponse(
    int Id,
    string Nome,
    string Endereco,
    int? Porta,
    string Endpoint,
    TipoProtocolo Tipo,
    string Url,
    int IntervaloSegundos,
    bool Ativo,
    StatusServico StatusAtual,
    int? UltimoTempoRespostaMs,
    DateTime? UltimaVerificacaoEm,
    DateTime? ProximaVerificacaoEm,
    DateTime CriadoEm,
    DateTime AtualizadoEm);

/// <summary>
/// Status atual de um serviço (GET /api/servicos/{id}/status).
/// </summary>
public record StatusServicoResponse(
    int ServicoId,
    string Nome,
    string Url,
    bool Ativo,
    StatusServico Status,
    int? TempoRespostaMs,
    int? CodigoHttp,
    DateTime? UltimaVerificacaoEm,
    string? MensagemErro);

/// <summary>
/// Indicadores de um serviço em um período (GET /api/servicos/{id}/metricas).
/// </summary>
public record MetricasServicoResponse(
    int ServicoId,
    string Periodo,
    DateTime Inicio,
    DateTime Fim,
    int TotalVerificacoes,
    int Online,
    int Offline,
    int Atencao,
    double? DisponibilidadePercentual,
    double? TempoMedioRespostaMs,
    int? TempoMinimoRespostaMs,
    int? TempoMaximoRespostaMs);

