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
    [HttpPost("bulk/{prestadorId}")]
    public async Task<IActionResult> UpsertAgenda(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda)
    {
        var success = await _service.UpsertAgendaAsync(prestadorId, agenda);
        return success ? Ok() : NotFound();
    }
}