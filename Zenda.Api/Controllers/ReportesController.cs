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

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardMetrics([FromQuery] DateTime inicio, [FromQuery] DateTime fin)
    {
        // Validación básica de fechas
        if (inicio > fin)
            return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin.");

        // Aseguramos que las fechas se traten como UTC en la base de datos
        var inicioUtc = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);

        // Extendemos el fin al último segundo del día para abarcar el día completo
        var finUtc = DateTime.SpecifyKind(fin, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1);

        try
        {
            var metricas = await _reporteService.GetDashboardMetricsAsync(inicioUtc, finUtc);
            return Ok(metricas);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
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