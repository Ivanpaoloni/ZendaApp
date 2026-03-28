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

    public TurnosController(ITurnosService turnosService)
    {
        _turnosService = turnosService;
    }
    
    [HttpPost]
    public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
    {
        var result = await _turnosService.ReservarTurnoAsync(dto);
        return Ok(result);
    }

    [HttpGet("prestador/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPrestador(Guid prestadorId)
    {
        return Ok(await _turnosService.GetByPrestadorAsync(prestadorId));
    }

    [HttpGet("disponibilidad/{prestadorId}")]
    public async Task<ActionResult<DisponibilidadFechaDto>> GetDisponibilidad(Guid prestadorId, [FromQuery] DateTime fecha)
    {
        return Ok(await _turnosService.GetDisponibilidadAsync(prestadorId, fecha));
    }

    [HttpGet("fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetTurnosPorFecha(DateTime fecha)
    {
        var turnos = await _turnosService.GetTurnosByFechaAsync(fecha);
        return Ok(turnos);
    }
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
}