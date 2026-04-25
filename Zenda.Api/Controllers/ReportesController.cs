using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Exclusivo para usuarios autenticados (Dueños/Administradores)
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;
    private readonly INegocioService _negocioService;
    private readonly ITenantService _tenantService;
    public ReportesController(IReporteService reporteService, INegocioService negocioService, ITenantService tenantService)
    {
        _reporteService = reporteService;
        _negocioService = negocioService;
        _tenantService = tenantService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics([FromQuery] DateTime inicio, [FromQuery] DateTime fin)
    {
        // 1. Obtenemos el ID del Tenant (Guid?)
        var negocioId = _tenantService.GetCurrentTenantId();

        // 2. Validamos si el ID es nulo antes de continuar
        if (!negocioId.HasValue)
        {
            return Unauthorized("No se pudo identificar el negocio del usuario.");
        }

        // 3. Pasamos negocioId.Value (que ya es Guid) al servicio
        var negocio = await _negocioService.GetByIdAsync(negocioId.Value);

        if (negocio?.PlanNombre == "Single")
        {
            return StatusCode(403, "Módulo no disponible para el plan actual.");
        }

        var metricas = await _reporteService.GetDashboardMetricsAsync(inicio, fin);
        return Ok(metricas);
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> ExportarExcel([FromQuery] DateTime inicio, [FromQuery] DateTime fin)
    {
        if (inicio > fin) return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin.");

        var inicioUtc = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fin, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1);

        try
        {
            var excelBytes = await _reporteService.GenerarReporteExcelAsync(inicioUtc, finUtc);

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MetricasZendy_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}