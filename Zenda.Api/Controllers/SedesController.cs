using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SedesController : ControllerBase
{
    private readonly ISedeService _service;

    public SedesController(ISedeService service)
    {
        _service = service;
    }

    [HttpGet]
    // Usamos el plural "Sedes" para ser consistentes con la ruta
    public async Task<ActionResult<IEnumerable<SedeReadDto>>> GetAll()
    {
        var sedes = await _service.GetAllAsync();
        return Ok(sedes);
    }
}