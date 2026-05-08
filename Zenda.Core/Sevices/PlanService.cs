using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;
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
        if (negocioId == null) return false;

        // 1. Buscamos la SUSCRIPCIÓN ACTIVA como Única Fuente de Verdad
        var suscripcionActiva = await _context.SuscripcionesNegocio
            .Include(s => s.PlanSuscripcion)
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId
                                   && s.Estado == EstadoSuscripcionEnum.Activa);

        // Si no hay suscripción activa o no tiene plan, bloqueamos la acción por seguridad (Fail-Fast)
        if (suscripcionActiva?.PlanSuscripcion == null) return false;

        // 2. Contamos los prestadores (Recordá que el filtro global en DbContext ya excluye los eliminados)
        var cantidadActual = await _context.Prestadores.CountAsync(p => p.NegocioId == negocioId);

        // 3. Validamos contra el límite REAL del plan activo
        return cantidadActual < suscripcionActiva.PlanSuscripcion.MaxProfesionales;
    }

    public async Task<bool> TieneRecordatoriosAutomaticosAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return false;

        // Misma lógica: consultamos la suscripción activa
        var suscripcionActiva = await _context.SuscripcionesNegocio
            .Include(s => s.PlanSuscripcion)
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId
                                   && s.Estado == EstadoSuscripcionEnum.Activa);

        return suscripcionActiva?.PlanSuscripcion?.HabilitaRecordatoriosHangfire ?? false;
    }

    public async Task<List<PlanVistaDto>> ObtenerPlanesActivosAsync()
    {
        var planes = await _context.PlanesSuscripcion.ToListAsync();

        return planes.Select(p => new PlanVistaDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            MaxSedes = p.MaxSedes,
            MaxProfesionales = p.MaxProfesionales,
            PrecioMensual = p.PrecioMensual,
            PrecioTexto = p.PrecioMensual == 0 ? "Gratis" : $"${p.PrecioMensual:N0}",
            HabilitaRecordatorios = p.HabilitaRecordatoriosHangfire
        })
        .OrderBy(p => p.PrecioMensual)
        .ToList();
    }
}