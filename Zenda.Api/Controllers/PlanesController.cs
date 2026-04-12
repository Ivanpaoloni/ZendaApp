using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class PlanesController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlanesController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet("puede-agregar-profesional")]
    public async Task<IActionResult> PuedeAgregarProfesional()
    {
        var puede = await _planService.PuedeAgregarProfesionalAsync();
        return Ok(puede);
    }

    [HttpGet("puede-agregar-sede")]
    public async Task<IActionResult> PuedeAgregarSede()
    {
        var puede = await _planService.PuedeAgregarProfesionalAsync();
        return Ok(puede);
    }

    [HttpGet("tiene-recordatorios")]
    public async Task<IActionResult> TieneRecordatorios()
    {
        var tiene = await _planService.TieneRecordatoriosAutomaticosAsync();
        return Ok(tiene);
    }
}