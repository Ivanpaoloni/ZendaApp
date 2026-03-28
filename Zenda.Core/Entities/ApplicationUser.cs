using Microsoft.AspNetCore.Identity;

namespace Zenda.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    // Clave para el Multi-Tenant: ¿A qué negocio pertenece este usuario?
    // Puede ser null si el día de mañana implementamos el SuperAdmin
    public Guid? NegocioId { get; set; }
    public Negocio? Negocio { get; set; }

    // Propiedad de navegación inversa (opcional, útil si queremos ir del User al Prestador)
    public Prestador? PrestadorVinculado { get; set; }
}
