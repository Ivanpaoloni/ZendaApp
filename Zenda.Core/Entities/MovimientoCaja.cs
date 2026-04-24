using Zenda.Core.Enums;
using Zenda.Core.Models;

namespace Zenda.Core.Entities
{
    public class MovimientoCaja : BaseEntity, ITenantEntity
    {
        public Guid NegocioId { get; set; }

        public Guid CajaDiariaId { get; set; }
        public CajaDiaria CajaDiaria { get; set; } = null!;

        public decimal Monto { get; set; } // PRECIO CONGELADO. Si el servicio sube de precio mañana, esto no cambia.

        public TipoMovimientoEnum Tipo { get; set; }
        public MedioPagoEnum MedioPago { get; set; }
        public string Detalle { get; set; } = string.Empty; // Ej: "Corte de Pelo - Juan Pérez" o "Compra de Insumos"

        // Relación Opcional (Si viene de un turno)
        public Guid? TurnoId { get; set; }
        public Turno? Turno { get; set; }
    }
}