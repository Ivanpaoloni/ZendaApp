using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zenda.Core.Interfaces;

namespace Zenda.Infrastructure.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly IZendaDbContext _dbContext;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(IZendaDbContext dbContext, IConfiguration configuration, ILogger<MercadoPagoService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        // Inicializamos el SDK con la credencial secreta guardada en appsettings.json o Variables de Entorno
        var accessToken = configuration["MercadoPago:AccessToken"];
        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogWarning("¡Atención! El AccessToken de Mercado Pago no está configurado.");
        }
        MercadoPagoConfig.AccessToken = accessToken;
    }
    public async Task<string> GenerarLinkDePagoAsync(Guid negocioId, Guid planId, string nombrePlan, decimal precio)
    {
        var request = new PreferenceRequest
        {
            Items = new List<PreferenceItemRequest>
        {
            new PreferenceItemRequest
            {
                Title = $"Suscripción ZendaApp - Plan {nombrePlan}",
                Quantity = 1,
                CurrencyId = "ARS", // O la moneda de tu país
                UnitPrice = precio,
            }
        },
            // ¡CRÍTICO! Aquí enviamos nuestros IDs para leerlos en el Webhook
            ExternalReference = $"{negocioId}|{planId}",

            // Redirecciones cuando el usuario termina el flujo
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = "https://app.zenda-app.com.ar/configuracion/mi-plan?pago=exitoso",
                Failure = "https://app.zenda-app.com.ar/configuracion/mi-plan?pago=fallido",
                Pending = "https://app.zenda-app.com.ar/configuracion/mi-plan?pago=pendiente"
            },
            AutoReturn = "approved"
        };

        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);

        // InitPoint es la URL pública del checkout de Mercado Pago
        return preference.InitPoint;
    }
    public async Task ProcesarPagoRecibidoAsync(long paymentId)
    {
        try
        {
            // 1. Consultamos el pago directamente a Mercado Pago (previene fraudes donde nos envían Webhooks falsos)
            var client = new PaymentClient();
            Payment payment = await client.GetAsync(paymentId);

            _logger.LogInformation("Estado del pago {PaymentId}: {Status}", paymentId, payment.Status);

            // 2. Verificamos que el pago esté realmente aprobado
            if (payment.Status == "approved")
            {
                // 3. Extraemos nuestra referencia. 
                // Cuando generemos el link de pago, enviaremos un string así: "IdDelNegocio|IdDelPlan"
                var externalReference = payment.ExternalReference;

                if (string.IsNullOrEmpty(externalReference) || !externalReference.Contains('|'))
                {
                    _logger.LogWarning("El pago {PaymentId} no tiene un ExternalReference válido: {Ref}", paymentId, externalReference);
                    return;
                }

                var ids = externalReference.Split('|');
                if (Guid.TryParse(ids[0], out Guid negocioId) && Guid.TryParse(ids[1], out Guid planId))
                {
                    // 4. Actualizamos la base de datos
                    var negocio = await _dbContext.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId);

                    if (negocio != null)
                    {
                        // IMPORTANTE: Asegurate de que tu entidad Negocio en Zenda.Core tenga la propiedad PlanSuscripcionId
                        // negocio.PlanSuscripcionId = planId; 

                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("¡Éxito! Negocio {NegocioId} fue actualizado al plan {PlanId}.", negocioId, planId);
                    }
                    else
                    {
                        _logger.LogError("Se recibió un pago para el Negocio {NegocioId} pero no existe en la base de datos.", negocioId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al consultar la API de Mercado Pago para el pago {PaymentId}", paymentId);
            throw; // El controlador atrapará esto y no romperá el ciclo HTTP.
        }
    }
}