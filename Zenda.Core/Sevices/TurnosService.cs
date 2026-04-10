using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class TurnosService : ITurnosService
{
    private readonly IZendaDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public TurnosService(IZendaDbContext context, IMapper mapper, ITenantService tenantService, IEmailService emailService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
        _emailService = emailService;
    }

    public async Task<TurnoReadDto> GetByIdAsync(Guid id)
    {
        var turno = await _context.Turnos.FindAsync(id);

        if (turno == null)
            return new();

        return _mapper.Map<TurnoReadDto>(turno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId)
    {
        var turnos = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId)
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha, Guid servicioId)
    {
        // 1. Traemos al prestador con Sede y Negocio
        var prestador = await _context.Prestadores
            .IgnoreQueryFilters()
            .Include(p => p.Sede)
            .Include(p => p.Negocio) // Incluimos Negocio
            .Select(p => new { p.Id, p.DuracionTurnoMinutos, p.Sede, p.Negocio }) // 🎯 FIX: Agregamos p.Negocio al Select
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        // 🎯 FIX: Faltaba el == null de prestador.Negocio
        if (prestador == null || prestador.Sede == null || prestador.Negocio == null)
            throw new Exception("Prestador, Sede o Negocio no encontrados");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // 🎯 FIX: Declaramos la variable que faltaba
        int anticipacionHoras = prestador.Negocio.AnticipacionMinimaHoras;

        // =================================================================
        // 2. ESCUDO: ¿El día consultado ya pasó en la vida real?
        // =================================================================
        var fechaHoraActualSede = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);
        var fechaActualSede = fechaHoraActualSede.Date;

        var respuesta = new DisponibilidadFechaDto { Fecha = fecha.Date };

        if (fecha.Date < fechaActualSede) return respuesta;

        // =================================================================
        //  MAGIA 1: Obtenemos la duración REAL del servicio elegido
        // =================================================================
        int duracionServicio = prestador.DuracionTurnoMinutos > 0 ? prestador.DuracionTurnoMinutos : 30; // Fallback

        var servicio = await _context.Servicios.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio != null && servicio.DuracionMinutos > 0)
        {
            duracionServicio = servicio.DuracionMinutos;
        }

        var inicioDiaLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        var finDiaLocal = inicioDiaLocal.AddDays(1);

        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaSede);
        var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaSede);

        int diaBuscado = (int)fecha.DayOfWeek;

        var configuracion = await _context.Disponibilidad
            .IgnoreQueryFilters()
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            .IgnoreQueryFilters()
            .Where(t => t.PrestadorId == prestadorId &&
                        t.FechaHoraInicioUtc >= inicioDiaUtc &&
                        t.FechaHoraInicioUtc < finDiaUtc &&
                        t.Estado != EstadoTurnoEnum.Cancelado)
            .Select(t => new { t.FechaHoraInicioUtc, t.FechaHoraFinUtc })
            .ToListAsync();

        var bloqueos = await _context.BloqueosAgenda
            .IgnoreQueryFilters()
            .Where(b => b.PrestadorId == prestadorId &&
                        b.InicioUtc < finDiaUtc &&
                        b.FinUtc > inicioDiaUtc)
            .ToListAsync();

        var horaActualSede = TimeOnly.FromDateTime(fechaHoraActualSede);

        // 🎯 Calculamos la barrera de tiempo real
        var barreraAnticipacion = fechaHoraActualSede.AddHours(anticipacionHoras);

        // Si el día entero que estamos buscando cae ANTES de la barrera, ni nos gastamos en iterar
        if (fecha.Date < barreraAnticipacion.Date) return respuesta;

        // Convertimos la barrera a TimeOnly para compararla con los slots dentro del día
        var horaMinimaPermitida = TimeOnly.FromDateTime(barreraAnticipacion);
        bool aplicaBarreraHoy = fecha.Date == barreraAnticipacion.Date;

        int intervaloGrillaMinutos = 15;

        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;
            var limiteFin = rango.HoraFin;

            while (inicioSlot.AddMinutes(duracionServicio) <= limiteFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracionServicio);

                // 🎯 FIX: Validamos que no esté muy pronto (reemplaza a yaPaso)
                bool muyPronto = aplicaBarreraHoy && inicioSlot <= horaMinimaPermitida;

                // Chequeo de Turnos 
                bool estaOcupado = turnosOcupados.Any(t =>
                {
                    var hInicioOcupado = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, zonaSede));
                    var hFinOcupado = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraFinUtc, zonaSede));
                    return hInicioOcupado < finSlot && inicioSlot < hFinOcupado;
                });

                // Chequeo de Bloqueos
                bool estaBloqueado = bloqueos.Any(b =>
                {
                    var hInicioBloqueo = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(b.InicioUtc, zonaSede));
                    var hFinBloqueo = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(b.FinUtc, zonaSede));
                    return hInicioBloqueo < finSlot && inicioSlot < hFinBloqueo;
                });

                // Si no está ocupado, no está bloqueado, y no es muy pronto:
                if (!estaOcupado && !estaBloqueado && !muyPronto)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                inicioSlot = inicioSlot.AddMinutes(intervaloGrillaMinutos);
            }
        }

        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == dto.ServicioId);

        if (servicio == null)
            throw new Exception("El servicio seleccionado no existe o no está disponible.");

        // 1. Buscamos el prestador con su Sede, Disponibilidad y Negocio
        var prestador = await _context.Prestadores
            .IgnoreQueryFilters()
            .Include(p => p.Sede)
            .Include(p => p.Horarios)
            .Include(p => p.Negocio) // 🎯 FIX: Traemos el negocio para leer la regla
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador?.Sede == null || prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador, Sede o Negocio no encontrados.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // Tratamos la fecha de entrada como "Hora del Local" y la pasamos a UTC
        var fechaCruda = DateTime.SpecifyKind(dto.Inicio, DateTimeKind.Unspecified);
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaCruda, zonaSede);
        var fechaFinUtcDefinitiva = fechaUtcDefinitiva.AddMinutes(servicio.DuracionMinutos);

        // ==========================================
        // BARRERAS DE VALIDACIÓN DEL NEGOCIO (BACKEND)
        // ==========================================

        int anticipacion = prestador.Negocio.AnticipacionMinimaHoras;

        // 🎯 BARRERA 1 MEJORADA: ¿Cumple con la anticipación?
        if (fechaUtcDefinitiva < DateTime.UtcNow.AddHours(anticipacion))
        {
            throw new InvalidOperationException($"Debe reservar con al menos {anticipacion} horas de anticipación.");
        }

        // BARRERA 2: ¿Está dentro del horario de trabajo de este barbero?
        int diaSemana = (int)fechaCruda.DayOfWeek;
        var horaSolicitada = TimeOnly.FromDateTime(fechaCruda);

        var horarioLaboral = prestador.Horarios.FirstOrDefault(h => h.DiaSemana == diaSemana);

        if (horarioLaboral == null ||
            horaSolicitada < horarioLaboral.HoraInicio ||
            horaSolicitada.AddMinutes(prestador.DuracionTurnoMinutos) > horarioLaboral.HoraFin)
        {
            throw new InvalidOperationException("El horario solicitado está fuera de la jornada laboral del profesional.");
        }

        // BARRERA 3: ¿El horario ya fue reservado por otra persona hace un milisegundo?
        bool turnoOcupado = await _context.Turnos.IgnoreQueryFilters().AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            t.Estado != EstadoTurnoEnum.Cancelado &&
            (fechaUtcDefinitiva < t.FechaHoraFinUtc && fechaFinUtcDefinitiva > t.FechaHoraInicioUtc)
        );

        if (turnoOcupado)
        {
            throw new InvalidOperationException("Lo sentimos, este horario acaba de ser reservado.");
        }

        // ==========================================
        // SI PASA TODAS LAS BARRERAS, GUARDAMOS
        // ==========================================

        var nuevoTurno = new Turno
        {
            NegocioId = prestador.NegocioId,
            PrestadorId = dto.PrestadorId,
            FechaHoraInicioUtc = fechaUtcDefinitiva,
            FechaHoraFinUtc = fechaFinUtcDefinitiva,
            NombreClienteInvitado = dto.NombreClienteInvitado,
            TelefonoClienteInvitado = dto.TelefonoClienteInvitado,
            EmailClienteInvitado = dto.EmailClienteInvitado,
            Estado = EstadoTurnoEnum.Confirmado,
            ServicioId = dto.ServicioId
        };

        _context.Turnos.Add(nuevoTurno);
        await _context.SaveChangesAsync();

        await _emailService.EnviarConfirmacionTurnoAsync(
            dto.EmailClienteInvitado,
            dto.NombreClienteInvitado,
            prestador.Negocio.Nombre,
            dto.Inicio);

        return _mapper.Map<TurnoReadDto>(nuevoTurno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        var inicioDiaLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        var finDiaLocal = inicioDiaLocal.AddDays(1);

        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaSede);
        var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaSede);

        var turnos = await _context.Turnos
        .AsNoTracking()
        .Include(t => t.Servicio)
        .Where(t => t.FechaHoraInicioUtc >= inicioDiaUtc && t.FechaHoraInicioUtc < finDiaUtc)
        .OrderBy(t => t.FechaHoraInicioUtc)
        .Select(t => new TurnoReadDto
        {
            Id = t.Id,
            NombreClienteInvitado = t.NombreClienteInvitado,
            TelefonoClienteInvitado = t.TelefonoClienteInvitado,
            EmailClienteInvitado = t.EmailClienteInvitado,

            PrestadorId = t.PrestadorId,
            PrestadorNombre = t.Prestador!.Nombre,

            ServicioId = t.ServicioId,
            ServicioNombre = t.Servicio.Nombre,
            Precio = t.Servicio.Precio,
            DuracionMinutos = t.Servicio.DuracionMinutos,

            FechaHoraInicioUtc = t.FechaHoraInicioUtc,
            FechaHoraFinUtc = t.FechaHoraFinUtc,
            Estado = t.Estado
        })
        .ToListAsync();

        return turnos;
    }

    public async Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.Prestador.NegocioId == negocioId);

        if (turno == null) return false;

        if (turno.Estado == EstadoTurnoEnum.Completado && nuevoEstado == EstadoTurnoEnum.Pendiente)
        {
            throw new Exception("No se puede reabrir un turno ya finalizado.");
        }

        turno.Estado = nuevoEstado;

        return await _context.SaveChangesAsync() > 0;
    }
}