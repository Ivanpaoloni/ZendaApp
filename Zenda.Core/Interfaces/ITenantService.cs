namespace Zenda.Core.Interfaces;

public interface ITenantService
{
    // Devuelve el ID del negocio del usuario actual, o null si no está logueado
    Guid? GetCurrentTenantId();
}