using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NegociosController : ControllerBase
{
    private readonly INegocioService _service;

    public NegociosController(INegocioService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NegocioReadDto>> GetById(Guid id)
    {
        var negocio = await _service.GetByIdAsync(id);
        return negocio == null ? NotFound() : Ok(negocio);
    }

    [HttpPost]
    public async Task<ActionResult<NegocioReadDto>> Create(NegocioCreateDto dto)
    {
        var nuevoNegocio = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = nuevoNegocio.Id }, nuevoNegocio);
    }
}