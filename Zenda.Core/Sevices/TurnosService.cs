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
    private readonly IJobService _jobService;

    public TurnosService(IZendaDbContext context, IMapper mapper, ITenantService tenantService, IEmailService emailService, IJobService jobService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
        _emailService = emailService;
        _jobService = jobService;
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
            .Include(p => p.Negocio)
            .Select(p => new { p.Id, p.DuracionTurnoMinutos, p.Sede, p.Negocio })
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null || prestador.Sede == null || prestador.Negocio == null)
            throw new Exception("Prestador, Sede o Negocio no encontrados");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);
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

        // Esta query está perfecta, trae los multi-días correctamente
        var bloqueos = await _context.BloqueosAgenda
            .IgnoreQueryFilters()
            .Where(b => b.PrestadorId == prestadorId &&
                        b.InicioUtc < finDiaUtc &&
                        b.FinUtc > inicioDiaUtc)
            .ToListAsync();

        var barreraAnticipacion = fechaHoraActualSede.AddHours(anticipacionHoras);

        if (fecha.Date < barreraAnticipacion.Date) return respuesta;

        var horaMinimaPermitida = TimeOnly.FromDateTime(barreraAnticipacion);
        bool aplicaBarreraHoy = fecha.Date == barreraAnticipacion.Date;

        // 🔥 CAMBIO CLAVE ACÁ: Leemos la configuración del negocio
        // Usamos el intervalo que eligió el dueño. Si por alguna razón el dato está en 0 (base corrupta), le damos 30 de fallback.
        int intervaloGrillaMinutos = prestador.Negocio.IntervaloTurnosMinutos > 0 ? prestador.Negocio.IntervaloTurnosMinutos : 30;

        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;
            var limiteFin = rango.HoraFin;

            while (inicioSlot.AddMinutes(duracionServicio) <= limiteFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracionServicio);

                // 🎯 REFACTOR CLAVE: Armamos la fecha+hora exacta del Slot y la pasamos a UTC
                var slotInicioLocal = inicioDiaLocal.Add(inicioSlot.ToTimeSpan());
                var slotFinLocal = inicioDiaLocal.Add(finSlot.ToTimeSpan());

                var slotInicioUtc = TimeZoneInfo.ConvertTimeToUtc(slotInicioLocal, zonaSede);
                var slotFinUtc = TimeZoneInfo.ConvertTimeToUtc(slotFinLocal, zonaSede);

                bool muyPronto = aplicaBarreraHoy && inicioSlot <= horaMinimaPermitida;

                // Chequeo de Turnos (Superposición exacta UTC)
                bool estaOcupado = turnosOcupados.Any(t =>
                    t.FechaHoraInicioUtc < slotFinUtc && slotInicioUtc < t.FechaHoraFinUtc
                );

                // Chequeo de Bloqueos (Superposición exacta UTC, soporta multi-día perfecto)
                bool estaBloqueado = bloqueos.Any(b =>
                    b.InicioUtc < slotFinUtc && slotInicioUtc < b.FinUtc
                );

                // Si no está ocupado, no está bloqueado, y no es muy pronto:
                if (!estaOcupado && !estaBloqueado && !muyPronto)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                // 🔥 USAMOS EL INTERVALO DINÁMICO ACÁ PARA AVANZAR EL SLOT
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

        var prestador = await _context.Prestadores
            .IgnoreQueryFilters()
            .Include(p => p.Sede)
            .Include(p => p.Horarios)
            .Include(p => p.Negocio)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador?.Sede == null || prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador, Sede o Negocio no encontrados.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        var fechaCruda = DateTime.SpecifyKind(dto.Inicio, DateTimeKind.Unspecified);
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaCruda, zonaSede);
        var fechaFinUtcDefinitiva = fechaUtcDefinitiva.AddMinutes(servicio.DuracionMinutos);

        // ==========================================
        // BARRERAS DE VALIDACIÓN DEL NEGOCIO (BACKEND)
        // ==========================================

        int anticipacion = prestador.Negocio.AnticipacionMinimaHoras;

        // BARRERA 1: Anticipación
        if (fechaUtcDefinitiva < DateTime.UtcNow.AddHours(anticipacion))
        {
            throw new InvalidOperationException($"Debe reservar con al menos {anticipacion} horas de anticipación.");
        }

        // BARRERA 2: Horario de trabajo
        int diaSemana = (int)fechaCruda.DayOfWeek;
        var horaSolicitada = TimeOnly.FromDateTime(fechaCruda);

        var horarioLaboral = prestador.Horarios.FirstOrDefault(h => h.DiaSemana == diaSemana);

        if (horarioLaboral == null ||
            horaSolicitada < horarioLaboral.HoraInicio ||
            horaSolicitada.AddMinutes(prestador.DuracionTurnoMinutos) > horarioLaboral.HoraFin)
        {
            throw new InvalidOperationException("El horario solicitado está fuera de la jornada laboral del profesional.");
        }

        // BARRERA 3: Choque de Turnos
        bool turnoOcupado = await _context.Turnos.IgnoreQueryFilters().AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            t.Estado != EstadoTurnoEnum.Cancelado &&
            (fechaUtcDefinitiva < t.FechaHoraFinUtc && fechaFinUtcDefinitiva > t.FechaHoraInicioUtc)
        );

        if (turnoOcupado)
        {
            throw new InvalidOperationException("Lo sentimos, este horario acaba de ser reservado.");
        }

        // 🎯 NUEVA BARRERA 4: Choque de Vacaciones/Bloqueos (Seguridad extra)
        bool chocaConBloqueo = await _context.BloqueosAgenda.IgnoreQueryFilters().AnyAsync(b =>
            b.PrestadorId == dto.PrestadorId &&
            b.InicioUtc < fechaFinUtcDefinitiva &&
            fechaUtcDefinitiva < b.FinUtc
        );

        if (chocaConBloqueo)
        {
            throw new InvalidOperationException("El horario solicitado se encuentra bloqueado por vacaciones o ausencia del profesional.");
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

        // FIX: Usamos la fecha UTC del turno para restar 1 hora
        var fechaRecordatorioUtc = fechaUtcDefinitiva.AddHours(-1);

        // Si la hora de mandar el recordatorio todavía está en el futuro...
        if (fechaRecordatorioUtc > DateTime.UtcNow)
        {
            var jobId = _jobService.ProgramarRecordatorioEmail(
                dto.EmailClienteInvitado,
                dto.NombreClienteInvitado,
                prestador.Negocio.Nombre,
                dto.Inicio,
                fechaRecordatorioUtc // Pasamos la fecha UTC correcta
            );
        }

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
        .Include(t => t.Prestador!.Sede)
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
            Estado = t.Estado,

            SedeNombre = t.Prestador!.Sede!.Nombre
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