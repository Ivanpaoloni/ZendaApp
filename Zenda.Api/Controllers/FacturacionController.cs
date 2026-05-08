using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FacturacionController : ControllerBase
{
    private readonly IFacturacionService _facturacionService;

    public FacturacionController(IFacturacionService facturacionService)
    {
        _facturacionService = facturacionService;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumenFacturacion()
    {
        var resumen = await _facturacionService.GetResumenAsync();

        if (resumen == null)
            return NotFound();

        return Ok(resumen);
    }
}