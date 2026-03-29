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
    }
}