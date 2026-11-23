using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitor.API.Data;
using Monitor.API.DTOs;
using Monitor.API.DTOs.Alertas;

namespace Monitor.API.Controllers;

/// <summary>
/// Consulta e reconhecimento de alertas gerados pelo monitoramento (Sprint 5).
/// </summary>
[ApiController]
[Route("api/alertas")]
[Produces("application/json")]
public class AlertasController(MonitorDbContext db) : ControllerBase
{
    /// <summary>Lista os alertas mais recentes (filtros opcionais por serviço e reconhecimento).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AlertaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int? servicoId = null,
        [FromQuery] bool? apenasNaoReconhecidos = null,
        [FromQuery] int limite = 50,
        CancellationToken ct = default)
    {
        var query = db.Alertas.AsNoTracking().Include(a => a.Servico).AsQueryable();

        if (servicoId.HasValue)
            query = query.Where(a => a.ServicoId == servicoId.Value);

        if (apenasNaoReconhecidos == true)
            query = query.Where(a => !a.Reconhecido);

        var alertas = await query
            .OrderBy(a => a.Reconhecido)
            .ThenByDescending(a => a.CriadoEm)
            .Take(Math.Clamp(limite, 1, 200))
            .ToListAsync(ct);

        return Ok(alertas.Select(a => a.ParaResponse()).ToList());
    }

    /// <summary>Marca um alerta específico como reconhecido pelo operador.</summary>
    [HttpPatch("{id:long}/reconhecer")]
    [ProducesResponseType<AlertaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reconhecer(long id, CancellationToken ct)
    {
        var alerta = await db.Alertas.Include(a => a.Servico).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alerta is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Alerta não encontrado",
                detail: $"Não existe alerta com o ID {id}.");
        }

        alerta.Reconhecido = true;
        await db.SaveChangesAsync(ct);
        return Ok(alerta.ParaResponse());
    }

    /// <summary>Marca todos os alertas pendentes como reconhecidos.</summary>
    [HttpPost("reconhecer-todos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReconhecerTodos([FromQuery] int? servicoId = null, CancellationToken ct = default)
    {
        var query = db.Alertas.Where(a => !a.Reconhecido);
        if (servicoId.HasValue)
            query = query.Where(a => a.ServicoId == servicoId.Value);

        await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.Reconhecido, true), ct);
        return NoContent();
    }
}
