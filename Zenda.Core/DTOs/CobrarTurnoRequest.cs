using Zenda.Core.Enums;

namespace Zenda.Core.DTOs
{
    public class CobrarTurnoRequest
    {
        public MedioPagoEnum MedioPago { get; set; }
        
        // Identificador físico de la sucursal que recibe el impacto en caja
        public Guid SedeCajaId { get; set; } 
    }
}