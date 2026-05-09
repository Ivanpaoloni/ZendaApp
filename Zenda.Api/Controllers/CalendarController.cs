using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly IExternalCalendarAuthService _authService;
    private readonly IMagicLinkService _magicLinkService;
    private readonly IPrestadoresService _prestadoresService;

    public CalendarController(
        IExternalCalendarAuthService authService,
        IMagicLinkService magicLinkService,
        IPrestadoresService prestadoresService)
    {
        _authService = authService;
        _magicLinkService = magicLinkService;
        _prestadoresService = prestadoresService;
    }

    [HttpGet("conectar-directo/{prestadorId}")]
    public IActionResult ConectarDirecto(int prestadorId)
    {
        // El prefijo DIR_ nos indica en el callback que es un flujo de integración directa
        string state = $"DIR_{prestadorId}";
        string url = _authService.GenerarUrlOAuth(state);
        return Ok(new { Url = url });
    }

    [HttpGet("generar-link/{prestadorId}")]
    public IActionResult GenerarLink(int prestadorId)
    {
        var token = _magicLinkService.GenerarTokenIntegracion(prestadorId);
        // Ruta configurada en tu cliente Blazor
        var url = $"https://app.zendy.com.ar/integracion/google?token={token}";
        return Ok(new { Url = url });
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            int prestadorId;

            if (state.StartsWith("DIR_"))
            {
                prestadorId = int.Parse(state.Replace("DIR_", ""));
            }
            else
            {
                var extractedId = _magicLinkService.ExtraerPrestadorId(state);
                if (extractedId == null) return BadRequest("Token de autorización inválido o expirado.");
                prestadorId = extractedId.Value;
            }

            var tokens = await _authService.IntercambiarCodigoAsync(code);
            await _prestadoresService.ActualizarGoogleTokenAsync(prestadorId, tokens.RefreshToken);

            // Redirección al frontend de Zendy con bandera de éxito
            return Redirect("https://app.zendy.com.ar/prestadores?sync=success");
        }
        catch (Exception)
        {
            return Redirect("https://app.zendy.com.ar/prestadores?sync=error");
        }
    }
}