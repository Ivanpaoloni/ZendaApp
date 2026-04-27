using Zenda.Core.Entities;
using Zenda.Core.Models;

public class SuscripcionNegocio : BaseEntity
{
    public Guid NegocioId { get; set; }
    public Negocio Negocio { get; set; } = null!;

    public Guid PlanSuscripcionId { get; set; }
    public PlanSuscripcion PlanSuscripcion { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoSuscripcionEnum Estado { get; set; } // Enum: Activa, Vencida, Cancelada, Pendiente

    public string? MercadoPagoPreapprovalId { get; set; } // Si aplica
    public decimal? PrecioMensualPersonalizado { get; set; }
}