using Microsoft.EntityFrameworkCore;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class PlanService : IPlanService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;

    public PlanService(IZendaDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> PuedeAgregarProfesionalAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        // Traemos el negocio CON sus límites de plan
        var negocio = await _context.Negocios
            .Include(n => n.PlanSuscripcion)
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        if (negocio?.PlanSuscripcion == null) return false;

        var cantidadActual = await _context.Prestadores.CountAsync(p => p.NegocioId == negocioId);

        return cantidadActual < negocio.PlanSuscripcion.MaxProfesionales;
    }

    public async Task<bool> TieneRecordatoriosAutomaticosAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var negocio = await _context.Negocios
            .Include(n => n.PlanSuscripcion)
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        return negocio?.PlanSuscripcion?.HabilitaRecordatoriosHangfire ?? false;
    }

    // ... mismo patrón para PuedeAgregarSedeAsync() ...
}