using Zenda.Core.Models;

namespace Zenda.Core.Entities
{
    public class Cliente : BaseEntity, ITenantEntity
    {
        public Guid NegocioId { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty; // Lo dejamos preparado por si en el futuro lo pedís separado
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Notas { get; set; }

        // Navegación: Un cliente tiene muchos turnos en este negocio
        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    }
}
