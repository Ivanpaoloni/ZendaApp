using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Application.Services;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly IServicioService _service;

    public ServiciosController(IServicioService service) => _service = service;
    [Authorize]
    [HttpGet("catalogo")]
    public async Task<ActionResult<IEnumerable<CategoriaServicioReadDto>>> GetCatalogo()
    {
        return Ok(await _service.GetCatalogoAsync());
    }
    [Authorize]
    [HttpPost("categorias")]
    public async Task<IActionResult> CreateCategoria(CategoriaServicioCreateDto dto)
    {
        return Ok(await _service.CreateCategoriaAsync(dto));
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateServicio(ServicioCreateDto dto)
    {
        return Ok(await _service.CreateServicioAsync(dto));
    }

    [AllowAnonymous]
    [HttpGet("publico/sede/{sedeId:guid}")]
    public async Task<ActionResult<IEnumerable<ServicioPublicoDto>>> GetPublicosPorSede(Guid sedeId)
    {
        var servicios = await _service.GetServiciosPublicosPorSedeAsync(sedeId);
        return Ok(servicios);
    }// Zenda.API/Controllers/ServiciosController.cs

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateServicio(Guid id, ServicioCreateDto dto)
    {
        var exito = await _service.UpdateServicioAsync(id, dto);
        if (!exito) return NotFound();
        return NoContent(); // 204 No Content es el estándar para un PUT exitoso
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteServicio(Guid id)
    {
        var exito = await _service.DeleteServicioAsync(id);
        if (!exito) return NotFound();
        return NoContent();
    }

    [Authorize]
    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> UpdateCategoria(Guid id, CategoriaServicioCreateDto dto)
    {
        var exito = await _service.UpdateCategoriaAsync(id, dto);
        if (!exito) return NotFound();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> DeleteCategoria(Guid id)
    {
        try
        {
            var exito = await _service.DeleteCategoriaAsync(id);
            if (!exito) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Devolvemos el error amigable si intentó borrar una categoría con servicios
            return BadRequest(ex.Message);
        }
    }
}