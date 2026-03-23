using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
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
        try
        {
            var nuevoTurno = await _turnosService.ReservarTurnoAsync(dto);
            return Ok(nuevoTurno);
        }
        catch (ArgumentException ex)
        {
            // Errores de lógica (ej: horario ocupado)
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Errores inesperados
            return StatusCode(500, new { message = "Ocurrió un error interno." });
        }
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
}