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

/// <summary>
/// Categoria do alerta gerado pela transição de status de um serviço (Fase 7 / Sprint 5).
/// </summary>
public enum TipoAlerta
{
    /// <summary>Serviço ficou indisponível (Offline).</summary>
    Indisponibilidade = 1,

    /// <summary>Serviço respondeu acima do limite de latência (Atenção).</summary>
    Lentidao = 2,

    /// <summary>Serviço voltou a responder normalmente (Online).</summary>
    Recuperacao = 3
}

/// <summary>
/// Perfil de acesso do usuário na plataforma.
/// - Admin: acesso total a todas as telas e operações (cadastro, edição, exclusão).
/// - Usuario: acesso controlado (permissões específicas definidas conforme evolução do projeto).
/// </summary>
public enum PerfilUsuario
{
    Admin = 1,
    Usuario = 2
}
