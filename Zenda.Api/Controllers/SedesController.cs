using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers; // Ajustá tu namespace

[ApiController]
[Route("api/[controller]")]
public class SedesController : ControllerBase
{
    private readonly ISedeService _service;

    public SedesController(ISedeService service) => _service = service;

    [AllowAnonymous] // Público para el flujo de reserva
    [HttpGet("public/negocio/{negocioId:guid}")]
    public async Task<ActionResult<IEnumerable<SedeReadDto>>> GetPublicByNegocio(Guid negocioId)
    {
        var sedes = await _service.GetPublicByNegocioIdAsync(negocioId);
        return Ok(sedes);
    }

    [Authorize] // Admin: Ver todas mis sedes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SedeReadDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [Authorize] // Admin: Crear sede
    [HttpPost]
    public async Task<ActionResult<SedeReadDto>> Create(SedeCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    // 🎯 NUEVO: Endpoint para Editar
    [Authorize] // Admin: Actualizar sede
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, SedeCreateDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);

        if (!actualizado) return NotFound();

        return NoContent(); // 204 No Content es el estándar para un PUT exitoso
    }

    [Authorize] // Admin: Borrar sede
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var eliminado = await _service.DeleteAsync(id);
            return eliminado ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // Esto devuelve un 400 con el texto plano "No se puede eliminar..."
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}