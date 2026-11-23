using Monitor.API.Models;

namespace Monitor.API.DTOs.Alertas;

public record AlertaResponse(
    long Id,
    int ServicoId,
    string ServicoNome,
    string ServicoUrl,
    TipoAlerta Tipo,
    StatusServico StatusAnterior,
    StatusServico StatusNovo,
    string Mensagem,
    bool Reconhecido,
    DateTime CriadoEm,
    DateTime? ResolvidoEm);
