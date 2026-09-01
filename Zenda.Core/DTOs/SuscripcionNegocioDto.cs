using Zenda.Core.Enums;

namespace Zenda.Core.DTOs
{
    public class SuscripcionNegocioDto
    {
        public Guid NegocioId { get; set; }
        public NegocioReadDto Negocio { get; set; } = null!;

        public Guid PlanSuscripcionId { get; set; }
        public PlanVistaDto PlanSuscripcion { get; set; } = null!;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public EstadoSuscripcionEnum Estado { get; set; }

        public string? MercadoPagoPreapprovalId { get; set; }
        public decimal? PrecioMensualPersonalizado { get; set; }
    }
}
