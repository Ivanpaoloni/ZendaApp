namespace Zenda.Core.Interfaces;

public interface IMercadoPagoService
{
    /// <summary>
    /// Consulta el pago en la API de Mercado Pago y actualiza el plan del negocio.
    /// </summary>
    Task<string> GenerarLinkDePagoAsync(Guid negocioId, Guid planId, string nombrePlan, decimal precio);
    Task ProcesarPagoRecibidoAsync(long paymentId);
    // Más adelante agregaremos: Task<string> GenerarLinkDePagoAsync(Guid negocioId, Guid planId);
}