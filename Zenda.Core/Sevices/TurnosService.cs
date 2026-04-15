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
            .OrderBy(d => d.HoraInicio)
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
        var horaFinSolicitada = horaSolicitada.AddMinutes(servicio.DuracionMinutos);

        // Validamos que el turno solicitado entre en ALGUNO de los rangos habilitados para ese día
        bool turnoDentroDeHorario = prestador.Horarios
            .Where(h => h.DiaSemana == diaSemana)
            .Any(h => horaSolicitada >= h.HoraInicio && horaFinSolicitada <= h.HoraFin);

        if (!turnoDentroDeHorario)
        {
            throw new InvalidOperationException("El horario solicitado está fuera de la jornada laboral o cae en un horario de descanso.");
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

        // 2. Mandamos el mail de confirmación inmediato

        await _emailService.EnviarConfirmacionTurnoAsync(
            dto.EmailClienteInvitado,
            dto.NombreClienteInvitado,
            prestador.Negocio.Nombre,
            dto.Inicio,
            nuevoTurno.Id,
            servicio.Nombre,
            prestador.Nombre,
            prestador.Sede.Nombre,
            prestador.Sede.Direccion
        );

        var fechaRecordatorioUtc = fechaUtcDefinitiva.AddHours(-1);

        // 3. Programamos el recordatorio y atajamos el JobId
        if (fechaRecordatorioUtc > DateTime.UtcNow)
        {
            var jobId = _jobService.ProgramarRecordatorioEmail(
                dto.EmailClienteInvitado,
                dto.NombreClienteInvitado,
                prestador.Negocio.Nombre,
                dto.Inicio,
                fechaRecordatorioUtc,
                nuevoTurno.Id
            );

            // 4. Segundo guardado: Le metemos el JobId al turno que ya existe
            nuevoTurno.RecordatorioJobId = jobId;
            await _context.SaveChangesAsync();
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

            SedeNombre = t.Prestador!.Sede!.Nombre,
            NegocioSlug = t.Prestador!.Negocio!.Slug
        })
        .ToListAsync();

        return turnos;
    }

    public async Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        // 🎯 MEJORA: Traemos Prestador, Sede y Negocio para poder armar el email de cancelación
        var turno = await _context.Turnos
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.Prestador.NegocioId == negocioId);

        if (turno == null) return false;

        if (turno.Estado == EstadoTurnoEnum.Completado && nuevoEstado == EstadoTurnoEnum.Pendiente)
        {
            throw new Exception("No se puede reabrir un turno ya finalizado.");
        }

        // 🎯 SI EL RECEPCIONISTA/DUEÑO CANCELA EL TURNO
        if (nuevoEstado == EstadoTurnoEnum.Cancelado && turno.Estado != EstadoTurnoEnum.Cancelado)
        {
            // 1. Matamos el recordatorio de Hangfire
            if (!string.IsNullOrEmpty(turno.RecordatorioJobId))
            {
                _jobService.CancelarTrabajo(turno.RecordatorioJobId);
                turno.RecordatorioJobId = null; // Lo limpiamos por prolijidad
            }

            // 2. Le avisamos al cliente por correo que el negocio le canceló la reserva
            if (!string.IsNullOrEmpty(turno.EmailClienteInvitado) && turno.Prestador?.Sede != null && turno.Prestador?.Negocio != null)
            {
                // Calculamos la hora local de la sede para el texto del correo
                var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(turno.Prestador.Sede.ZonaHorariaId);
                var inicioSedeLocal = TimeZoneInfo.ConvertTimeFromUtc(turno.FechaHoraInicioUtc, zonaSede);

                await _emailService.EnviarCancelacionTurnoAsync(
                    turno.EmailClienteInvitado,
                    turno.NombreClienteInvitado,
                    turno.Prestador.Negocio.Nombre,
                    inicioSedeLocal,
                    turno.Prestador.Negocio.Slug
                );
            }
        }

        turno.Estado = nuevoEstado;

        return await _context.SaveChangesAsync() > 0;
    }

    // Para la vista pública de gestión
    public async Task<TurnoReadDto> GetResumenPublicoAsync(Guid turnoId)
    {
        var turno = await _context.Turnos
            .IgnoreQueryFilters()
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            // NUEVO: Traemos el negocio para sacar el Slug
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .Include(t => t.Servicio)
            .FirstOrDefaultAsync(t => t.Id == turnoId);

        if (turno == null) return null;

        return new TurnoReadDto
        {
            Id = turno.Id,
            FechaHoraInicioUtc = turno.FechaHoraInicioUtc,
            Estado = turno.Estado,
            NombreClienteInvitado = turno.NombreClienteInvitado,
            PrestadorNombre = turno.Prestador.Nombre,
            ServicioNombre = turno.Servicio.Nombre,
            SedeNombre = turno.Prestador.Sede?.Nombre ?? "",
            NegocioSlug = turno.Prestador.Negocio?.Slug ?? ""
        };
    }

    // Para la acción de cancelar desde el mail
    public async Task<bool> CancelarPorClienteAsync(Guid turnoId)
    {
        // Juntamos la búsqueda del turno y los datos del negocio en una sola query
        var turno = await _context.Turnos
            .IgnoreQueryFilters()
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .FirstOrDefaultAsync(t => t.Id == turnoId);

        if (turno == null) return false;

        if (turno.Prestador?.Sede == null || turno.Prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador, Sede o Negocio no encontrados.");

        // Validaciones de tiempo
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(turno.Prestador.Sede.ZonaHorariaId);
        var ahoraSede = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);
        var inicioSedeLocal = TimeZoneInfo.ConvertTimeFromUtc(turno.FechaHoraInicioUtc, zonaSede);

        // TODO: PARAMETRIZAR
        int horasMinimas = 2;
        if (inicioSedeLocal <= ahoraSede.AddHours(horasMinimas))
        {
            throw new Exception($"No es posible cancelar el turno, la anticipación minima es de {horasMinimas} horas. Por favor comuníquese por teléfono.");
        }

        if (turno.Estado == EstadoTurnoEnum.Completado)
        {
            throw new Exception("El turno ya figura como completado.");
        }

        // Si pasa todo, cancelamos
        turno.Estado = EstadoTurnoEnum.Cancelado;
        var exito = await _context.SaveChangesAsync() > 0;

        // 🎯 SI SE GUARDÓ LA CANCELACIÓN, ENVIAMOS EL EMAIL
        if (exito && !string.IsNullOrEmpty(turno.EmailClienteInvitado))
        {
            await _emailService.EnviarCancelacionTurnoAsync(
                turno.EmailClienteInvitado,
                turno.NombreClienteInvitado,
                turno.Prestador.Negocio.Nombre,
                inicioSedeLocal, // Le pasamos la hora local para que el email no lo confunda
                turno.Prestador.Negocio.Slug
            );

            // 🎯 MAGIA: ELIMINAMOS EL RECORDATORIO DE HANGFIRE
            if (!string.IsNullOrEmpty(turno.RecordatorioJobId))
            {
                // Llamamos a tu método existente
                _jobService.CancelarTrabajo(turno.RecordatorioJobId);

                turno.RecordatorioJobId = null;
                await _context.SaveChangesAsync();
            }
        }

        return exito;
    }
}