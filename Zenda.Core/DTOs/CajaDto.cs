using Zenda.Core.Enums;

namespace Zenda.Core.DTOs
{
    public class CajaDiariaDto
    {
        public Guid Id { get; set; }
        public DateTime FechaCaja { get; set; }
        public decimal MontoInicial { get; set; }
        public bool EstaAbierta { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal SaldoActual { get; set; }
        public List<MovimientoCajaDto> Movimientos { get; set; } = new();
    }

    public class MovimientoCajaDto
    {
        public Guid Id { get; set; }
        public decimal Monto { get; set; }
        public TipoMovimientoEnum Tipo { get; set; }
        public MedioPagoEnum MedioPago { get; set; }
        public string Detalle { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class NuevoMovimientoDto
    {
        public Guid SedeId { get; set; }
        public decimal Monto { get; set; }
        public TipoMovimientoEnum Tipo { get; set; }
        public MedioPagoEnum MedioPago { get; set; }
        public string Detalle { get; set; } = string.Empty;
    }
}