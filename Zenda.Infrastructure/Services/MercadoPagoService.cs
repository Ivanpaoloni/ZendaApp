using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.Preference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zenda.Core.Enums;
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

        //inicializar sdk
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
                    Title = $"Suscripción Zendy - Plan {nombrePlan}",
                    Quantity = 1,
                    CurrencyId = "ARS",
                    UnitPrice = precio,
                }
            },
            ExternalReference = $"{negocioId}|{planId}",
            BackUrls = new PreferenceBackUrlsRequest
            {
                Success = "https://app.zendy.com.ar/configuracion/plan?pago=exitoso",
                Failure = "https://app.zendy.com.ar/configuracion/plan?pago=fallido",
                Pending = "https://app.zendy.com.ar/configuracion/plan?pago=pendiente"
            },
            AutoReturn = "approved"
        };

        var client = new PreferenceClient();
        Preference preference = await client.CreateAsync(request);

        //para pruebas pasar a sandbox
        return preference.InitPoint; 
    }
    public async Task ProcesarPagoRecibidoAsync(long paymentId)
    {
        try
        {
            var client = new PaymentClient();
            Payment payment = await client.GetAsync(paymentId);

            _logger.LogInformation("Estado del pago {PaymentId}: {Status}", paymentId, payment.Status);

            if (payment.Status == "approved")
            {
                var externalReference = payment.ExternalReference;

                if (string.IsNullOrEmpty(externalReference) || !externalReference.Contains('|'))
                {
                    _logger.LogWarning("El pago {PaymentId} no tiene un ExternalReference válido: {Ref}", paymentId, externalReference);
                    return;
                }

                var ids = externalReference.Split('|');
                if (Guid.TryParse(ids[0], out Guid negocioId) && Guid.TryParse(ids[1], out Guid planId))
                {
                    // buscar negocio y suscripcion si tuviera
                    var negocio = await _dbContext.Negocios.FirstOrDefaultAsync(n => n.Id == negocioId);
                    if (negocio == null)
                    {
                        _logger.LogError("Pago recibido para el Negocio {NegocioId} pero no existe.", negocioId);
                        return;
                    }

                    var suscripcion = await _dbContext.SuscripcionesNegocio.FirstOrDefaultAsync(s => s.NegocioId == negocioId);

                    // renovacion o nuevo contrato
                    if (suscripcion == null)
                    {
                        // si es primer pago
                        suscripcion = new SuscripcionNegocio
                        {
                            NegocioId = negocioId,
                            PlanSuscripcionId = planId,
                            FechaInicio = DateTime.UtcNow,
                            FechaVencimiento = DateTime.UtcNow.AddMonths(1), //por defecto 1 mes, luego parametrizar
                            Estado = EstadoSuscripcionEnum.Activa
                        };
                        _dbContext.SuscripcionesNegocio.Add(suscripcion);
                    }
                    else
                    {
                        // si ya tenia se actualiza
                        suscripcion.PlanSuscripcionId = planId;
                        suscripcion.Estado = EstadoSuscripcionEnum.Activa;

                        // si estaba en deuda se renueva desde la fecha actual
                        if (suscripcion.FechaVencimiento < DateTime.UtcNow) suscripcion.FechaVencimiento = DateTime.UtcNow.AddMonths(1);
                        // si paga por adelantado se suma un mes al vto original
                        else suscripcion.FechaVencimiento = suscripcion.FechaVencimiento.AddMonths(1);

                        _dbContext.SuscripcionesNegocio.Update(suscripcion);
                    }
                    // registrar historial de pagos
                    var montoPagado = payment.TransactionAmount ?? 0m;

                    var historial = new HistorialPago
                    {
                        SuscripcionNegocio = suscripcion,
                        MontoCobrado = montoPagado,
                        FechaPago = DateTime.UtcNow,
                        MercadoPagoPaymentId = paymentId.ToString(),
                        DetalleRecibo = "Aprobado vía MP"
                    };

                    _dbContext.HistorialPagos.Add(historial);

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("¡Éxito! Negocio {NegocioId} procesó el pago y actualizó suscripción al plan {PlanId}.", negocioId, planId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al consultar la API de Mercado Pago para el pago {PaymentId}", paymentId);
            throw;
        }
    }
}