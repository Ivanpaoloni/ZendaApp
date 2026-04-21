using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
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

    public async Task<List<PlanVistaDto>> ObtenerPlanesActivosAsync()
    {
        // 🎯 AHORA SÍ: Buscamos los planes correctamente en la base de datos
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
        .OrderBy(p => p.PrecioMensual) // Los ordenamos por precio (de Menor a Mayor)
        .ToList();
    }
}