using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class DisponibilidadController : ControllerBase
{
    private readonly IDisponibilidadService _service;

    public DisponibilidadController(IDisponibilidadService service)
    {
        _service = service;
    }

    [HttpGet("prestador/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<DisponibilidadReadDto>>> GetByPrestador(Guid prestadorId)
    {
        return Ok(await _service.GetByPrestadorAsync(prestadorId));
    }

    [HttpPost]
    public async Task<ActionResult<DisponibilidadReadDto>> Create(DisponibilidadCreateDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    // Nuevo: Útil para la pantalla de configuración de agenda del profesional
    [HttpPost("upsert/{prestadorId}")]
    public async Task<IActionResult> UpsertAgenda(Guid prestadorId, [FromBody] IEnumerable<DisponibilidadCreateDto> agenda)
    {
        var resultado = await _service.UpsertAgendaAsync(prestadorId, agenda);
        return resultado ? NoContent() : BadRequest("No se pudo actualizar la agenda.");
    }
   
    [HttpGet("bloqueos/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<BloqueoReadDto>>> GetBloqueos(Guid prestadorId)
    {
        return Ok(await _service.GetBloqueosFuturosAsync(prestadorId));
    }

    [HttpPost("bloqueos")]
    public async Task<ActionResult> CreateBloqueo(BloqueoCreateDto dto)
    {
        try
        {
            var result = await _service.CrearBloqueoAsync(dto);
            return result ? Ok() : BadRequest(new { message = "Error al guardar." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("bloqueos/{id}")]
    public async Task<IActionResult> DeleteBloqueo(Guid id)
    {
        var success = await _service.EliminarBloqueoAsync(id);
        return success ? NoContent() : NotFound();
    }
    // En DisponibilidadController.cs
    [HttpGet("bloqueos/hoy")]
    public async Task<ActionResult<IEnumerable<BloqueoReadDto>>> GetBloqueosDeHoy()
    {
        var bloqueos = await _service.GetBloqueosDeHoyAsync();

        // Siempre devolvemos 200 OK. 
        // Si es null, devolvemos una lista vacía para que el frontend no se asuste.
        return Ok(bloqueos ?? new List<BloqueoReadDto>());
    }
}