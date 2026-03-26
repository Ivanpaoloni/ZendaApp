using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Sede : BaseEntity, ITenantEntity
{
    public Guid NegocioId { get; set; }
    public Negocio? Negocio { get; set; }

    public string Nombre { get; set; } = string.Empty; // Ej: "Local Palermo"

    public List<Prestador> Prestadores { get; set; } = new();
}