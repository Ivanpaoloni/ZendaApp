using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
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
    private readonly IConfiguration _config;

    public CalendarController(
        IExternalCalendarAuthService authService,
        IMagicLinkService magicLinkService,
        IPrestadoresService prestadoresService,
        IConfiguration config)
    {
        _authService = authService;
        _magicLinkService = magicLinkService;
        _prestadoresService = prestadoresService;
        _config = config;
    }

    [HttpGet("conectar-directo/{prestadorId:guid}")]
    public IActionResult ConectarDirecto(Guid prestadorId)
    {
        string state = $"DIR_{prestadorId}";
        string url = _authService.GenerarUrlOAuth(state);
        return Ok(new { Url = url });
    }

    [HttpGet("generar-link/{prestadorId:guid}")]
    public IActionResult GenerarLink(Guid prestadorId)
    {
        var token = _magicLinkService.GenerarTokenIntegracion(prestadorId);
        var url = $"https://app.zendy.com.ar/integracion/google?token={token}";
        return Ok(new { Url = url });
    }

    // NUEVO ENDPOINT: Desvincular Calendario
    [HttpDelete("desvincular/{prestadorId:guid}")]
    public async Task<IActionResult> Desvincular(Guid prestadorId)
    {
        try
        {
            await _prestadoresService.DesvincularGoogleCalendarAsync(prestadorId);
            return Ok(new { Message = "Calendario desvinculado exitosamente." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Prestador no encontrado.");
        }
        catch (Exception)
        {
            return StatusCode(500, "Error interno al intentar desvincular el calendario.");
        }
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? state)
    {
        var frontendUrl = _config["FrontendUrl"];

        // 1. Si el usuario canceló o hubo un error de Google
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return Redirect($"{frontendUrl}/prestadores?sync=error");
        }

        try
        {
            // ==========================================
            // 2. RECUPERAR EL ID DEL PRESTADOR
            // ==========================================
            // El state llega como "DIR_019dbfce-8d66..."
            Guid prestadorId = Guid.Empty;

            if (!string.IsNullOrEmpty(state) && state.StartsWith("DIR_"))
            {
                var idString = state.Substring(4); // Le cortamos los primeros 4 caracteres ("DIR_")
                Guid.TryParse(idString, out prestadorId);
            }

            if (prestadorId == Guid.Empty)
            {
                // Si alguien manipuló el link o el state viene vacío
                return Redirect($"{frontendUrl}/prestadores?sync=error");
            }

            // ==========================================
            // 3. INTERCAMBIAR EL CÓDIGO POR EL TOKEN
            // ==========================================
            // Llamamos al método mágico que me acabas de mostrar
            // 3. INTERCAMBIAR EL CÓDIGO POR EL TOKEN (¡Ahora atrapamos 3 variables!)
            var (refreshToken, email, calendarId) = await _authService.IntercambiarCodigoAsync(code);

            // 4. GUARDAR EN LA BASE DE DATOS (¡Le pasamos el calendarId al servicio!)

            // ==========================================
            // 4. GUARDAR EN LA BASE DE DATOS
            // ==========================================
            // TODO: Aquí debes llamar a tu servicio para guardar los datos en Zenda.
            // Ejemplo:
            await _prestadoresService.ActualizarGoogleTokenAsync(prestadorId, refreshToken, calendarId);

            // 5. ¡Éxito! Redirigimos al frontend
            return Redirect($"{frontendUrl}/prestadores?sync=success");
        }
        catch (InvalidOperationException ex)
        {
            // Esta atrapa tu excepción personalizada de "Google no devolvió un Refresh Token"
            Console.WriteLine($"Error de Token: {ex.Message}");
            return Redirect($"{frontendUrl}/prestadores?sync=error&motivo=no_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error interno en callback: {ex.Message}");
            return Redirect($"{frontendUrl}/prestadores?sync=error");
        }
    }
}