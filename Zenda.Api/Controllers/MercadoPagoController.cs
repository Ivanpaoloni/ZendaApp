using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
 // ¡CRÍTICO! Mercado Pago no enviará nuestro JWT, este endpoint debe ser público
public class MercadoPagoController : ControllerBase
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly ILogger<MercadoPagoController> _logger;
    private readonly ITenantService _tenantService;

    public MercadoPagoController(IMercadoPagoService mercadoPagoService, ILogger<MercadoPagoController> logger, ITenantService tenantService)
    {
        _mercadoPagoService = mercadoPagoService;
        _logger = logger;
        _tenantService = tenantService;
    }

    [Authorize] // Solo usuarios autenticados pueden generar links
    [HttpPost("generar-link")]
    public async Task<IActionResult> GenerarLink([FromBody] GenerarLinkDto request)
    {
        // Obtener el Tenant (Negocio) actual del usuario logueado
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Unauthorized();

        var url = await _mercadoPagoService.GenerarLinkDePagoAsync(
            negocioId.Value,
            request.PlanId,
            request.NombrePlan,
            request.Precio
        );

        return Ok(new { UrlCheckout = url });
    }
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> RecibirNotificacion([FromBody] MercadoPagoWebhookDto payload, [FromQuery] string? topic, [FromQuery] long? id)
    {
        _logger.LogInformation("Webhook recibido de Mercado Pago. Tipo: {Type}", payload.Type ?? topic);

        try
        {
            // Mercado Pago puede enviar por body o por query params dependiendo de la versión de la API
            long? paymentId = payload.Data?.Id ?? (topic == "payment" ? id : null);

            if ((payload.Type == "payment" || topic == "payment") && paymentId.HasValue)
            {
                // Procesamos el pago. En un SaaS de altísimo volumen, esto se encolaría en Hangfire.
                // Por ahora, un await directo está perfecto.
                await _mercadoPagoService.ProcesarPagoRecibidoAsync(paymentId.Value);
            }

            // SIEMPRE retornar 200 OK inmediatamente.
            // Si retornamos 400/500, Mercado Pago reintentará enviar la notificación por días.
            return Ok(new { message = "Notificación procesada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando el webhook de Mercado Pago");
            // Aún si falla nuestra lógica interna, solemos devolver 200 a MP,
            // pero internamente registramos el log para revisarlo y reprocesar manualmente.
            return Ok();
        }
    }
}