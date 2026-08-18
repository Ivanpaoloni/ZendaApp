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

    public async Task<bool> PuedeCrearTurnoAsync(DateTime fechaHoraInicioTurno)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        if (negocioId == null) 
            return false;

        SuscripcionNegocio? suscripcionActiva = await ObtenerSuscripcionActiva(negocioId);

        if (suscripcionActiva?.PlanSuscripcion == null) 
            return false;

        if (suscripcionActiva.PlanSuscripcion.Nombre != "Single")
            return true;

        //var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        //var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        //var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        // la hardcodeo para no consultarla al pedo
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        var fechaTurnoLocal = TimeZoneInfo.ConvertTimeFromUtc(fechaHoraInicioTurno, zonaSede);
        var inicioMesLocal = new DateTime(fechaTurnoLocal.Year, fechaTurnoLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioMesSiguienteLocal = inicioMesLocal.AddMonths(1);

        var inicioMesUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesLocal, zonaSede);
        var finMesUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesSiguienteLocal, zonaSede);

        var turnosDelMes = await _context.Turnos
            .CountAsync(t => t.NegocioId == negocioId
                          && t.FechaHoraInicioUtc >= inicioMesUtc
                          && t.FechaHoraInicioUtc < finMesUtc
                          && t.Estado != EstadoTurnoEnum.Cancelado);

        return turnosDelMes < 50;
    }

    private async Task<SuscripcionNegocio?> ObtenerSuscripcionActiva(Guid? negocioId)
    {
        var suscripcion = await _context.SuscripcionesNegocio.Include(s => s.PlanSuscripcion).FirstOrDefaultAsync(s => s.NegocioId == negocioId && s.Estado == EstadoSuscripcionEnum.Activa);
        return suscripcion;
    }
}