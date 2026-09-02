using AutoMapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class PlanService : IPlanService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public PlanService(IZendaDbContext context, ITenantService tenantService, IMapper mapper)
    {
        _context = context;
        _tenantService = tenantService;
        _mapper = mapper;
    }

    public async Task<bool> PuedeAgregarProfesionalAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return false;

        // 1. Buscamos la SUSCRIPCIÓN ACTIVA como Única Fuente de Verdad
        var suscripcionActiva = await ObtenerSuscripcionActiva(negocioId);

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
        var suscripcionActiva = await ObtenerSuscripcionActiva(negocioId);

        return suscripcionActiva?.PlanSuscripcion?.HabilitaRecordatoriosHangfire ?? false;
    }

    public async Task<List<PlanVistaDto>> ObtenerPlanesActivosAsync()
    {
        var planes = await _context.PlanesSuscripcion.ToListAsync();

        var planesActivos = planes.Select(p => new PlanVistaDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            MaxSedes = p.MaxSedes,
            MaxProfesionales = p.MaxProfesionales,
            PrecioMensual = p.PrecioMensual,
            PrecioTexto = p.PrecioMensual == 0 ? "Gratis" : $"${p.PrecioMensual:N0}",
            HabilitaRecordatorios = p.HabilitaRecordatoriosHangfire
        }).OrderBy(p => p.PrecioMensual).ToList();

        return planesActivos;
    }

    public async Task<bool> PuedeCrearTurnoAsync(Guid negocioId, DateTime fechaTurno)
    {
        // ELIMINAMOS la dependencia de _tenantService aquí.

        var suscripcion = await ObtenerSuscripcionActivaByNegocioId(negocioId);

        // Permite la reserva si está activa o en sus 7 días de Gracia
        bool tienePermisoPorSuscripcion = suscripcion != null && (suscripcion.EsSuscripcionActiva || suscripcion.EsPeriodoDeGracia);

        if (!tienePermisoPorSuscripcion)
            return false;

        // Validación de límites para el plan "Free"
        if (suscripcion.PlanSuscripcion != null && suscripcion.PlanSuscripcion.PrecioMensual == 0)
        {
            var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var totalTurnosMes = await _context.Turnos
                .IgnoreQueryFilters() // CRÍTICO: Debe contar turnos sin importar quién llama al endpoint
                .CountAsync(t => t.NegocioId == negocioId &&
                                 t.FechaHoraInicioUtc >= inicioMes &&
                                 t.Estado != EstadoTurnoEnum.Cancelado);

            return totalTurnosMes < 50;
        }

        return true;
    }

    public async Task<SuscripcionNegocioDto?> ObtenerSuscripcionActivaByNegocioId(Guid negocioId)
    {
        var suscripcion = await ObtenerSuscripcionActiva(negocioId);

        return _mapper.Map<SuscripcionNegocioDto>(suscripcion);
    }

    private async Task<SuscripcionNegocio?> ObtenerSuscripcionActiva(Guid? negocioId)
    {
        if (negocioId == null) return null;

        var ultimaSuscripcion = await _context.SuscripcionesNegocio
            .Include(s => s.PlanSuscripcion)
            .OrderByDescending(s => s.FechaVencimiento)
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

        if (ultimaSuscripcion == null) return null;

        bool tienePermisosOperativos = ultimaSuscripcion.EsSuscripcionActiva || ultimaSuscripcion.EsPeriodoDeGracia;

        if (!tienePermisosOperativos)
        {
            return null;
        }

        return ultimaSuscripcion;
    }
}