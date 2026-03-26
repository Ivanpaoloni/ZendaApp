using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.API.Controllers; // Asegurate de poner el namespace de tu proyecto API

[ApiController]
[Route("api/[controller]")]
public class PrestadoresController : ControllerBase
{
    private readonly IPrestadoresService _service;

    public PrestadoresController(IPrestadoresService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrestadorReadDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    // CORRECCIÓN: Cambiamos de slug a id, y agregamos la restricción :guid
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrestadorReadDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound(new { message = "Prestador no encontrado" }) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PrestadorReadDto>> Create(PrestadorCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);

        // CORRECCIÓN: Redirigimos al método GetById usando el Id del nuevo prestador
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PrestadorUpdateDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}