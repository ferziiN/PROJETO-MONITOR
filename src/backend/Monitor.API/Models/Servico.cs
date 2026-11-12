namespace Monitor.API.Models;

/// <summary>
/// Serviço (HTTP/HTTPS) cadastrado para monitoramento.
/// </summary>
public class Servico
{
    public int Id { get; set; }

    /// <summary>Nome amigável utilizado para identificação (ex.: "API Principal").</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>IP ou domínio, sem protocolo (ex.: "api.exemplo.com" ou "192.168.0.10").</summary>
    public string Endereco { get; set; } = string.Empty;

    /// <summary>Porta do serviço. Quando nula, usa a porta padrão do protocolo (80/443).</summary>
    public int? Porta { get; set; }

    /// <summary>Caminho utilizado na verificação (ex.: "/health").</summary>
    public string Endpoint { get; set; } = "/";

    public TipoProtocolo Tipo { get; set; } = TipoProtocolo.Https;

    /// <summary>Frequência das verificações, em segundos.</summary>
    public int IntervaloSegundos { get; set; } = 60;

    /// <summary>Define se o monitoramento está habilitado.</summary>
    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    // ---------------------------------------------------------------------
    // Estado atual (desnormalizado a partir da última verificação).
    // Evita consultar o histórico inteiro para montar o dashboard.
    // ---------------------------------------------------------------------

    public StatusServico StatusAtual { get; set; } = StatusServico.Desconhecido;
    public int? UltimoTempoRespostaMs { get; set; }
    public DateTime? UltimaVerificacaoEm { get; set; }

    /// <summary>Momento a partir do qual o agendador deve verificar o serviço novamente.</summary>
    public DateTime? ProximaVerificacaoEm { get; set; }

    public ICollection<Verificacao> Verificacoes { get; set; } = new List<Verificacao>();

    /// <summary>
    /// Monta a URL completa que será consultada (ex.: https://api.exemplo.com:443/health).
    /// </summary>
    public Uri ObterUrl()
    {
        var builder = new UriBuilder
        {
            Scheme = Tipo == TipoProtocolo.Https ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            Host = Endereco,
            Port = Porta ?? -1, // -1 = porta padrão do protocolo
            Path = Endpoint
        };

        return builder.Uri;
    }
}

