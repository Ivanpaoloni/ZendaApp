using Zenda.Core.Models;

namespace Zenda.Core.Entities
{
    public class CajaDiaria : BaseEntity, ITenantEntity
    {
        public Guid NegocioId { get; set; }
        public Guid SedeId { get; set; }
        public Sede Sede { get; set; } = null!;

        public DateTime FechaCaja { get; set; } // Representa el día (Ej: 14/04/2026)
        public decimal MontoInicial { get; set; } // Cambio en efectivo al abrir
        public decimal MontoFinalDeclarado { get; set; } // Lo que el usuario cuenta físicamente al cerrar
        public DateTime? FechaCierreUtc { get; set; }
        public bool EstaAbierta { get; set; } = true;

        public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();
    }
}