using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;

[ApiController]
[Route("api/[controller]")]
public class NegociosController : ControllerBase
{
    private readonly INegocioService _service;

    public NegociosController(INegocioService service) => _service = service;

    [AllowAnonymous]
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<NegocioReadDto>> GetPublicBySlug(string slug)
    {
        var negocio = await _service.GetPublicBySlugAsync(slug);
        return negocio == null ? NotFound() : Ok(negocio);
    }

    [Authorize] // Solo el dueño logueado
    [HttpGet("perfil")]
    public async Task<ActionResult<NegocioReadDto>> GetPerfil()
    {
        var result = await _service.GetPerfilAsync();
        return result == null ? NotFound("Perfil no encontrado") : Ok(result);
    }

    [Authorize] // Gestión administrativa
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NegocioReadDto>> GetById(Guid id)
    {
        var negocio = await _service.GetByIdAsync(id);
        return negocio == null ? NotFound() : Ok(negocio);
    }

    [Authorize] // Solo personal autorizado
    [HttpPost]
    public async Task<ActionResult<NegocioReadDto>> Create(NegocioCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}