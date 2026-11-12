using Monitor.API.Models;

namespace Monitor.API.DTOs.Verificacoes;

public record VerificacaoResponse(
    long Id,
    int ServicoId,
    StatusServico Status,
    int? TempoRespostaMs,
    int? CodigoHttp,
    DateTime VerificadoEm,
    string? MensagemErro);

