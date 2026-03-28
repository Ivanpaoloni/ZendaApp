using Microsoft.AspNetCore.Http;
using Zenda.Core.Interfaces;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null || !user.Identity!.IsAuthenticated)
            return null;

        // Buscamos el claim que vamos a crear más adelante llamado "NegocioId"
        var tenantClaim = user.FindFirst("NegocioId")?.Value;

        if (Guid.TryParse(tenantClaim, out Guid tenantId))
            return tenantId;

        return null;
    }
}