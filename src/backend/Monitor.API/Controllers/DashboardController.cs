using Microsoft.AspNetCore.Mvc;
using Monitor.API.DTOs.Dashboard;
using Monitor.API.Services;

namespace Monitor.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>Resumo geral: totais por status e lista de serviços com o estado atual.</summary>
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obter(CancellationToken ct)
        => Ok(await dashboardService.ObterResumoAsync(ct));
}

