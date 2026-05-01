using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Application.Services;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class TurnosController : ControllerBase
{
    private readonly ITurnosService _turnosService;
    private readonly IEmailService _emailService;

    public TurnosController(ITurnosService turnosService, IEmailService emailService)
    {
        _turnosService = turnosService;
        _emailService = emailService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
    {
        // 1. Tu lógica actual de guardar en la DB...
        var nuevoTurno = await _turnosService.ReservarTurnoAsync(dto);

        // 2. Mandamos el mail "en segundo plano" (sin bloquear la respuesta)
        // No usamos 'await' acá si no queremos que el cliente espere a que el mail salga,
        // pero por ahora para probar, usalo:
        
        return nuevoTurno;
    }

    [HttpGet("prestador/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPrestador(Guid prestadorId)
    {
        return Ok(await _turnosService.GetByPrestadorAsync(prestadorId));
    }
   
    [AllowAnonymous]
    [HttpGet("disponibilidad")]
    public async Task<ActionResult<DisponibilidadFechaDto>> GetDisponibilidad([FromQuery] Guid sedeId,[FromQuery] DateTime fecha,[FromQuery] Guid servicioId,[FromQuery] Guid? prestadorId = null)
    {
        return Ok(await _turnosService.GetDisponibilidadAsync(prestadorId, sedeId, fecha, servicioId));
    }
    [HttpGet("fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetTurnosPorFecha(DateTime fecha)
    {
        var turnos = await _turnosService.GetTurnosByFechaAsync(fecha);
        return Ok(turnos);
    }
    [Authorize]
    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> UpdateEstado(Guid id, [FromBody] EstadoTurnoEnum nuevoEstado)
    {
        try
        {
            var exito = await _turnosService.CambiarEstadoAsync(id, nuevoEstado);
            if (!exito) return NotFound("Turno no encontrado o no pertenece a su negocio.");

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [AllowAnonymous]
    [HttpPatch("{id}/cancelar-cliente")]
    public async Task<IActionResult> CancelarTurnoCliente(Guid id)
    {
        try
        {
            // Usamos un nuevo método en el servicio que no chequee el TenantId
            var exito = await _turnosService.CancelarPorClienteAsync(id);
            if (!exito) return NotFound("Turno no encontrado.");

            return Ok(new { success = true, message = "Turno cancelado exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}/resumen")]
    public async Task<ActionResult<TurnoReadDto>> GetResumenTurnoPublico(Guid id)
    {
        // Necesitamos un método que traiga los datos para mostrar en la pantalla de gestión
        var turno = await _turnosService.GetResumenPublicoAsync(id);
        if (turno == null) return NotFound();
        return Ok(turno);
    }

    [Authorize]
    [HttpGet("dashboard/resumen")]
    public async Task<ActionResult<DashboardResumenDto>> GetDashboardResumen()
    {
        var resumen = await _turnosService.GetDashboardResumenAsync();
        return Ok(resumen);
    }
    [Authorize]
    [HttpPost("{id}/cobrar")]
    public async Task<IActionResult> CobrarTurno(Guid id, [FromBody] CobrarTurnoRequest request)
    {
        try
        {
            var exito = await _turnosService.FinalizarYCobrarTurnoAsync(id, request.MedioPago);
            if (exito) return Ok();

            return BadRequest("No se pudo procesar el cobro.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("exportar")]
    public async Task<IActionResult> ExportarTurnos([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var excelBytes = await _turnosService.GenerarReporteExcelAsync(desde, hasta);

        return File(excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Turnos_Zendy_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    [HttpPost("admin")]
    [Authorize] // Exclusivo para administradores/recepcionistas logueados
    public async Task<ActionResult<TurnoReadDto>> CrearTurnoAdmin([FromBody] TurnoAdminCreateDto dto)
    {
        try
        {
            var resultado = await _turnosService.CrearTurnoAdminAsync(dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            // Atrapamos los choques de horario o validaciones de negocio
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Errores generales
            return StatusCode(500, new { message = "Ocurrió un error interno al intentar agendar el turno." });
        }
    }
    public class CobrarTurnoRequest
    {
        public Zenda.Core.Enums.MedioPagoEnum MedioPago { get; set; }
    }
}