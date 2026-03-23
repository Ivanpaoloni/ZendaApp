using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class TurnosService : ITurnosService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public TurnosService(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId)
    {
        var turnos = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId)
            .OrderBy(t => t.Inicio)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha)
    {
        var prestador = await _context.Prestadores
            .Select(p => new { p.Id, p.DuracionTurnoMinutos })
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null) throw new Exception("Prestador no encontrado");

        // SEGURIDAD: Si la duración es 0 o negativa, el bucle sería infinito.
        int duracion = prestador.DuracionTurnoMinutos > 0 ? prestador.DuracionTurnoMinutos : 30;

        int diaBuscado = (int)fecha.DayOfWeek;
        var fechaFiltro = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);

        var configuracion = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId && t.Inicio.Date == fechaFiltro)
            .Select(t => new { t.Inicio, t.Fin }) // Traemos solo lo necesario
            .ToListAsync();

        var respuesta = new DisponibilidadFechaDto { Fecha = fechaFiltro };

        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;
            var limiteFin = rango.HoraFin;

            // Mientras el slot actual + duración quepa en el rango de atención
            while (inicioSlot.AddMinutes(duracion) <= limiteFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracion);

                // Verificamos solapamiento
                bool estaOcupado = turnosOcupados.Any(t =>
                    TimeOnly.FromDateTime(t.Inicio) < finSlot &&
                    inicioSlot < TimeOnly.FromDateTime(t.Fin));

                if (!estaOcupado)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                // AVANCE CRÍTICO: 
                // Si finSlot es menor o igual a inicioSlot (por error de datos o medianoche), 
                // rompemos para evitar el bucle infinito.
                if (finSlot <= inicioSlot) break;

                inicioSlot = finSlot;
            }
        }
        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        // 1. Obtener datos del prestador y normalizar fechas
        var prestador = await _context.FindAsync<Prestador>(dto.PrestadorId);
        if (prestador == null) throw new Exception("Prestador no encontrado.");

        var inicioLimpio = DateTime.SpecifyKind(new DateTime(
            dto.Inicio.Year, dto.Inicio.Month, dto.Inicio.Day,
            dto.Inicio.Hour, dto.Inicio.Minute, 0), DateTimeKind.Utc);

        var finSolicitado = inicioLimpio.AddMinutes(prestador.DuracionTurnoMinutos);

        var horaInicio = TimeOnly.FromDateTime(inicioLimpio);
        var horaFin = TimeOnly.FromDateTime(finSolicitado);

        // 2. VALIDACIÓN 1: ¿Atiende en ese rango horario?
        var atiende = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == (int)inicioLimpio.DayOfWeek &&
            horaInicio >= d.HoraInicio &&
            horaFin <= d.HoraFin);

        if (!atiende)
            throw new ArgumentException("El profesional no atiende en el horario o día solicitado.");

        // 3. VALIDACIÓN 2: ¿Hay solapamiento con otros turnos?
        var ocupado = await _context.Turnos.AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            inicioLimpio < t.Fin &&
            t.Inicio < finSolicitado);

        if (ocupado)
            throw new ArgumentException("El horario ya se encuentra ocupado.");

        // 4. Mapeo, asignación de valores finales y persistencia
        var turno = _mapper.Map<Turno>(dto);
        turno.Id = Guid.NewGuid();
        turno.Inicio = inicioLimpio;
        turno.Fin = finSolicitado;
        turno.EstaConfirmado = false; // Por defecto arrancan sin confirmar

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        return _mapper.Map<TurnoReadDto>(turno);
    }
}