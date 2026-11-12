namespace Monitor.API.Models;

/// <summary>
/// Protocolo utilizado na verificação de disponibilidade do serviço.
/// </summary>
public enum TipoProtocolo
{
    Http = 1,
    Https = 2
}

/// <summary>
/// Estado de um serviço monitorado (ESCOPO-PROJETO.md, seção 7).
/// </summary>
public enum StatusServico
{
    /// <summary>Serviço ainda não foi verificado.</summary>
    Desconhecido = 0,

    /// <summary>Serviço respondeu corretamente à verificação.</summary>
    Online = 1,

    /// <summary>Serviço não respondeu, retornou erro ou estourou o timeout.</summary>
    Offline = 2,

    /// <summary>Serviço respondeu, mas com tempo de resposta acima do limite configurado.</summary>
    Atencao = 3
}

