using Microsoft.AspNetCore.Mvc;
using Monitor.API.DTOs.Servicos;
using Monitor.API.DTOs.Verificacoes;
using Monitor.API.Models;
using Monitor.API.Services;
using Monitor.API.Services.Monitoramento;

namespace Monitor.API.Controllers;

/// <summary>
/// CRUD de serviços monitorados + consultas de status, histórico e métricas.
/// </summary>
[ApiController]
[Route("api/servicos")]
[Produces("application/json")]
public class ServicosController(
    IServicoService servicoService,
    IMonitoramentoService monitoramentoService) : ControllerBase
{
    /// <summary>Lista os serviços cadastrados (filtros opcionais: ativo, status).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServicoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool? ativo, [FromQuery] StatusServico? status, CancellationToken ct)
        => Ok(await servicoService.ListarAsync(ativo, status, ct));

    /// <summary>Busca um serviço pelo ID.</summary>
    [HttpGet("{id:int}", Name = nameof(ObterPorId))]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken ct)
    {
        var servico = await servicoService.ObterPorIdAsync(id, ct);
        return servico is null ? ServicoNaoEncontrado(id) : Ok(servico);
    }

    /// <summary>Cadastra um novo serviço. A primeira verificação ocorre no próximo ciclo.</summary>
    [HttpPost]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarServicoRequest request, CancellationToken ct)
    {
        var criado = await servicoService.CriarAsync(request, ct);
        return CreatedAtRoute(nameof(ObterPorId), new { id = criado.Id }, criado);
    }

    /// <summary>Atualiza todos os dados de um serviço.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarServicoRequest request, CancellationToken ct)
    {
        var atualizado = await servicoService.AtualizarAsync(id, request, ct);
        return atualizado is null ? ServicoNaoEncontrado(id) : Ok(atualizado);
    }

    /// <summary>Ativa ou desativa o monitoramento do serviço.</summary>
    [HttpPatch("{id:int}/ativo")]
    [ProducesResponseType<ServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarMonitoramento(int id, [FromBody] AlterarMonitoramentoRequest request, CancellationToken ct)
    {
        var atualizado = await servicoService.AlterarMonitoramentoAsync(id, request.Ativo, ct);
        return atualizado is null ? ServicoNaoEncontrado(id) : Ok(atualizado);
    }

    /// <summary>Remove o serviço e todo o seu histórico de verificações.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
        => await servicoService.RemoverAsync(id, ct) ? NoContent() : ServicoNaoEncontrado(id);

    /// <summary>Status atual do serviço (com base na última verificação).</summary>
    [HttpGet("{id:int}/status")]
    [ProducesResponseType<StatusServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterStatus(int id, CancellationToken ct)
    {
        var status = await servicoService.ObterStatusAsync(id, ct);
        return status is null ? ServicoNaoEncontrado(id) : Ok(status);
    }

    /// <summary>Histórico de verificações, mais recentes primeiro.</summary>
    /// <param name="limite">Quantidade máxima de registros (1–1000). Padrão: 50.</param>
    /// <param name="desde">Retorna apenas verificações a partir desta data (ISO 8601).</param>
    [HttpGet("{id:int}/verificacoes")]
    [ProducesResponseType<IReadOnlyList<VerificacaoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarVerificacoes(
        int id, [FromQuery] int limite = 50, [FromQuery] DateTime? desde = null, CancellationToken ct = default)
    {
        var verificacoes = await servicoService.ListarVerificacoesAsync(id, limite, desde, ct);
        return verificacoes is null ? ServicoNaoEncontrado(id) : Ok(verificacoes);
    }

    /// <summary>Executa uma verificação imediata (fora do agendamento).</summary>
    [HttpPost("{id:int}/verificar")]
    [ProducesResponseType<VerificacaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerificarAgora(int id, CancellationToken ct)
    {
        var resultado = await monitoramentoService.VerificarAsync(id, ct);
        return resultado is null ? ServicoNaoEncontrado(id) : Ok(resultado);
    }

    /// <summary>Disponibilidade (%) e tempos de resposta em um período.</summary>
    /// <param name="periodo">Ex.: 30m, 1h, 24h, 7d, 30d (máx. 90d). Padrão: 24h.</param>
    [HttpGet("{id:int}/metricas")]
    [ProducesResponseType<MetricasServicoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterMetricas(int id, [FromQuery] string periodo = "24h", CancellationToken ct = default)
    {
        if (!ServicoService.TentarConverterPeriodo(periodo, out var duracao))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Período inválido",
                detail: "Use o formato <número><m|h|d>, ex.: 30m, 24h, 7d (máximo 90d).");
        }

        var metricas = await servicoService.ObterMetricasAsync(id, duracao, periodo.ToLowerInvariant(), ct);
        return metricas is null ? ServicoNaoEncontrado(id) : Ok(metricas);
    }

    private ObjectResult ServicoNaoEncontrado(int id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Serviço não encontrado",
        detail: $"Não existe serviço com o ID {id}.");
}

