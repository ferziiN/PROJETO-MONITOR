using System.ComponentModel.DataAnnotations;
using Monitor.API.Models;

namespace Monitor.API.DTOs.Servicos;

/// <summary>
/// Campos comuns ao cadastro e à atualização de um serviço.
/// </summary>
public abstract record ServicoRequestBase
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    public string Nome { get; init; } = string.Empty;

    /// <summary>IP ou domínio, sem protocolo e sem caminho (ex.: api.exemplo.com).</summary>
    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(255)]
    [RegularExpression(@"^[A-Za-z0-9]([A-Za-z0-9\-\.]*[A-Za-z0-9])?$",
        ErrorMessage = "Informe apenas o IP ou domínio, sem 'http://', porta ou caminho (ex.: api.exemplo.com).")]
    public string Endereco { get; init; } = string.Empty;

    /// <summary>Porta opcional. Quando omitida, usa 80 (HTTP) ou 443 (HTTPS).</summary>
    [Range(1, 65535, ErrorMessage = "A porta deve estar entre 1 e 65535.")]
    public int? Porta { get; init; }

    /// <summary>Caminho da verificação. Deve começar com "/" (ex.: /health).</summary>
    [StringLength(500)]
    [RegularExpression(@"^/.*$", ErrorMessage = "O endpoint deve começar com '/' (ex.: /health).")]
    public string? Endpoint { get; init; } = "/";

    [Required(ErrorMessage = "O tipo (Http ou Https) é obrigatório.")]
    [EnumDataType(typeof(TipoProtocolo), ErrorMessage = "Tipo inválido. Use 'Http' ou 'Https'.")]
    public TipoProtocolo Tipo { get; init; } = TipoProtocolo.Https;

    [Range(10, 86400, ErrorMessage = "O intervalo deve estar entre 10 segundos e 24 horas (86400).")]
    public int IntervaloSegundos { get; init; } = 60;

    public bool Ativo { get; init; } = true;
}

public record CriarServicoRequest : ServicoRequestBase;

public record AtualizarServicoRequest : ServicoRequestBase;

/// <summary>
/// Corpo do PATCH /api/servicos/{id}/ativo — liga/desliga o monitoramento.
/// </summary>
public record AlterarMonitoramentoRequest(bool Ativo);

