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
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public TurnosService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
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
            // Actualizado a la propiedad real de la entidad
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha)
    {
        // 1. Traemos al prestador
        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .Select(p => new { p.Id, p.DuracionTurnoMinutos, p.Sede })
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null || prestador.Sede == null) throw new Exception("Prestador o Sede no encontrados");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // =================================================================
        // 2. NUEVO ESCUDO: ¿El día consultado ya pasó en la vida real?
        // =================================================================
        var fechaHoraActualSede = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);
        var fechaActualSede = fechaHoraActualSede.Date;

        // Inicializamos la respuesta vacía
        var respuesta = new DisponibilidadFechaDto { Fecha = fecha.Date };

        // Si la fecha que me piden es MENOR a hoy, corto acá y devuelvo la lista vacía.
        if (fecha.Date < fechaActualSede)
        {
            return respuesta;
        }
        // =================================================================

        // Si pasamos el escudo, seguimos con la lógica normal...
        int duracion = prestador.DuracionTurnoMinutos > 0 ? prestador.DuracionTurnoMinutos : 30;

        var inicioDiaLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        var finDiaLocal = inicioDiaLocal.AddDays(1);

        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaSede);
        var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaSede);

        int diaBuscado = (int)fecha.DayOfWeek;

        var configuracion = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId &&
                        t.FechaHoraInicioUtc >= inicioDiaUtc &&
                        t.FechaHoraInicioUtc < finDiaUtc &&
                        t.Estado != EstadoTurnoEnum.Cancelado)
            .Select(t => new { t.FechaHoraInicioUtc, t.FechaHoraFinUtc })
            .ToListAsync();

        // 3. Evaluamos la hora actual solo si es "Hoy"
        var horaActualSede = TimeOnly.FromDateTime(fechaHoraActualSede);
        bool esHoy = fecha.Date == fechaActualSede;

        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;
            var limiteFin = rango.HoraFin;

            while (inicioSlot.AddMinutes(duracion) <= limiteFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracion);

                // Verificamos si la hora ya pasó HOY
                bool yaPaso = esHoy && inicioSlot <= horaActualSede;

                bool estaOcupado = turnosOcupados.Any(t =>
                {
                    var hInicioOcupado = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, zonaSede));
                    var hFinOcupado = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraFinUtc, zonaSede));

                    return hInicioOcupado < finSlot && inicioSlot < hFinOcupado;
                });

                if (!estaOcupado && !yaPaso)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                inicioSlot = finSlot;
                if (duracion <= 0) break;
            }
        }

        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        // 1. Buscamos el prestador con su Sede y su Disponibilidad
        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador?.Sede == null)
            throw new InvalidOperationException("Prestador o Sede no encontrados.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // Tratamos la fecha de entrada como "Hora del Local" y la pasamos a UTC
        var fechaCruda = DateTime.SpecifyKind(dto.Inicio, DateTimeKind.Unspecified);
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaCruda, zonaSede);
        var fechaFinUtcDefinitiva = fechaUtcDefinitiva.AddMinutes(prestador.DuracionTurnoMinutos);

        // ==========================================
        // BARRERAS DE VALIDACIÓN DEL NEGOCIO (BACKEND)
        // ==========================================

        // BARRERA 1: ¿El turno es en el pasado?
        if (fechaUtcDefinitiva < DateTime.UtcNow)
        {
            throw new InvalidOperationException("No se pueden reservar turnos en el pasado.");
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
        bool turnoOcupado = await _context.Turnos.AnyAsync(t =>
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
            Estado = EstadoTurnoEnum.Confirmado
        };

        _context.Turnos.Add(nuevoTurno);
        await _context.SaveChangesAsync();

        return _mapper.Map<TurnoReadDto>(nuevoTurno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha)
    {
        // 1. Extraemos solo la fecha (00:00:00) y la forzamos explícitamente a UTC
        var fechaInicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
        var fechaFin = fechaInicio.AddDays(1);

        var turnosDb = await _context.Turnos
            .AsNoTracking()
            .Include(t => t.Prestador)
            .Where(t => t.FechaHoraInicioUtc >= fechaInicio && t.FechaHoraInicioUtc < fechaFin)
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        var turnosDto = _mapper.Map<List<TurnoReadDto>>(turnosDb);

        return turnosDto;
    }

    public async Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado)
    {
        // 1. Buscamos el turno asegurándonos que pertenezca al NegocioId del token actual
        // (Asumo que tenés un _tenantService o similar para obtener el NegocioId actual)
        var negocioId = _tenantService.GetCurrentTenantId();

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.Prestador.NegocioId == negocioId);

        if (turno == null) return false;

        // 2. Lógica de transición (Opcional pero recomendado)
        // Ejemplo: Si ya está completado, no podés volverlo a pendiente
        if (turno.Estado == EstadoTurnoEnum.Completado && nuevoEstado == EstadoTurnoEnum.Pendiente)
        {
            throw new Exception("No se puede reabrir un turno ya finalizado.");
        }

        // 3. Aplicamos el cambio
        turno.Estado = nuevoEstado;

        return await _context.SaveChangesAsync() > 0;
    }
}