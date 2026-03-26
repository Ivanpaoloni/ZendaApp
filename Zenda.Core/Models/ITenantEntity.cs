namespace Zenda.Core.Models;

/// <summary>
/// Interfaz obligatoria para cualquier tabla que pertenezca a un cliente del SaaS.
/// Garantiza que Entity Framework pueda filtrar automáticamente los datos por negocio.
/// </summary>
public interface ITenantEntity
{
    Guid NegocioId { get; set; }
}