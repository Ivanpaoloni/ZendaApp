using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class PrestadoresController : ControllerBase
{
    private readonly IPrestadoresService _service;

    public PrestadoresController(IPrestadoresService service) => _service = service;

    [AllowAnonymous] // Público: Ver staff de una sede para reservar
    [HttpGet("public/sede/{sedeId:guid}")]
    public async Task<ActionResult<IEnumerable<PrestadorReadDto>>> GetPublicBySede(Guid sedeId)
    {
        var result = await _service.GetPublicBySedeIdAsync(sedeId);
        return Ok(result);
    }

    [Authorize] // Admin: Mi lista de staff
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrestadorReadDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PrestadorReadDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PrestadorReadDto>> Create(PrestadorCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PrestadorUpdateDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}