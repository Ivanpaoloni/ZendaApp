using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

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

    [HttpGet("{slug}")]
    public async Task<ActionResult<PrestadorReadDto>> GetBySlug(string slug)
    {
        var result = await _service.GetBySlugAsync(slug);
        return result == null ? NotFound(new { message = "Prestador no encontrado" }) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PrestadorReadDto>> Create(PrestadorCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, PrestadorUpdateDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}