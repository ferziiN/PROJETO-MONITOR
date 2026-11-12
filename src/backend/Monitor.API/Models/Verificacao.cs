namespace Monitor.API.Models;

/// <summary>
/// Resultado de uma verificação de disponibilidade (histórico — RF05).
/// </summary>
public class Verificacao
{
    public long Id { get; set; }

    public int ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public StatusServico Status { get; set; }

    /// <summary>Tempo de resposta em milissegundos. Nulo quando não houve resposta.</summary>
    public int? TempoRespostaMs { get; set; }

    /// <summary>Código HTTP retornado (ex.: 200, 503). Nulo quando não houve resposta.</summary>
    public int? CodigoHttp { get; set; }

    public DateTime VerificadoEm { get; set; }

    /// <summary>Descrição do erro (timeout, DNS, conexão recusada, HTTP 500...).</summary>
    public string? MensagemErro { get; set; }
}

