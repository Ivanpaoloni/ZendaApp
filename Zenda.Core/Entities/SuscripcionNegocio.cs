using Zenda.Core.Entities;
using Zenda.Core.Enums;
using Zenda.Core.Models;

public class SuscripcionNegocio : BaseEntity
{
    public Guid NegocioId { get; set; }
    public Negocio Negocio { get; set; } = null!;

    public Guid PlanSuscripcionId { get; set; }
    public PlanSuscripcion PlanSuscripcion { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoSuscripcionEnum Estado { get; set; }

    public string? MercadoPagoPreapprovalId { get; set; }
    public decimal? PrecioMensualPersonalizado { get; set; }
    public bool EsPeriodoDeGracia
    {
        get
        {
            if (Estado != EstadoSuscripcionEnum.Activa) 
                return false;

            if (PlanSuscripcion != null && PlanSuscripcion.PrecioMensual == 0) 
                return false;

            var hoy = DateTime.UtcNow;
            return FechaVencimiento < hoy && FechaVencimiento.AddDays(7) >= hoy;
        }
    }
    public bool EsSuscripcionActiva
    {
        get
        {
            if (Estado != EstadoSuscripcionEnum.Activa) 
                return false;

            if (PlanSuscripcion != null && PlanSuscripcion.PrecioMensual == 0) 
                return true;

            return FechaVencimiento >= DateTime.UtcNow;
        }
    }
}