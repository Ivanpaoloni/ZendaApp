using Zenda.Core.Models;

public class HistorialPago : BaseEntity
{
    public Guid SuscripcionNegocioId { get; set; }
    public SuscripcionNegocio SuscripcionNegocio { get; set; } = null!;

    public decimal MontoCobrado { get; set; }
    public DateTime FechaPago { get; set; }
    public string MercadoPagoPaymentId { get; set; } = string.Empty;

    // Podrías guardar un JSON con el response de MP por si necesitas debuggear
    public string? DetalleRecibo { get; set; }
}