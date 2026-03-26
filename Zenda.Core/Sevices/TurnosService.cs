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
            // Actualizado a la propiedad real de la entidad
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid prestadorId, DateTime fecha)
    {
        var prestador = await _context.Prestadores
            .Select(p => new { p.Id, p.DuracionTurnoMinutos })
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null) throw new Exception("Prestador no encontrado");

        int duracion = prestador.DuracionTurnoMinutos > 0 ? prestador.DuracionTurnoMinutos : 30;

        int diaBuscado = (int)fecha.DayOfWeek;
        var fechaFiltroInicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
        var fechaFiltroFin = fechaFiltroInicio.AddDays(1);

        var configuracion = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            // Filtramos usando las propiedades UTC reales
            .Where(t => t.PrestadorId == prestadorId &&
                        t.FechaHoraInicioUtc >= fechaFiltroInicio &&
                        t.FechaHoraInicioUtc < fechaFiltroFin &&
                        t.Estado != "Cancelado") // Buena práctica: ignorar turnos cancelados en la disponibilidad
            .Select(t => new { t.FechaHoraInicioUtc, t.FechaHoraFinUtc })
            .ToListAsync();

        var respuesta = new DisponibilidadFechaDto { Fecha = fechaFiltroInicio };

        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;
            var limiteFin = rango.HoraFin;

            while (inicioSlot.AddMinutes(duracion) <= limiteFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracion);

                bool estaOcupado = turnosOcupados.Any(t =>
                    TimeOnly.FromDateTime(t.FechaHoraInicioUtc) < finSlot &&
                    inicioSlot < TimeOnly.FromDateTime(t.FechaHoraFinUtc));

                if (!estaOcupado)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                if (finSlot <= inicioSlot) break;
                inicioSlot = finSlot;
            }
        }
        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        // 1. Validamos que exista el prestador para heredar su NegocioId y conocer la duración
        var prestador = await _context.Prestadores.FindAsync(dto.PrestadorId);
        if (prestador == null) throw new Exception("Prestador no encontrado.");

        var inicioLimpio = DateTime.SpecifyKind(new DateTime(
            dto.Inicio.Year, dto.Inicio.Month, dto.Inicio.Day,
            dto.Inicio.Hour, dto.Inicio.Minute, 0), DateTimeKind.Utc);

        var finSolicitado = inicioLimpio.AddMinutes(prestador.DuracionTurnoMinutos);

        var horaInicio = TimeOnly.FromDateTime(inicioLimpio);
        var horaFin = TimeOnly.FromDateTime(finSolicitado);

        // 2. ¿Atiende en ese rango?
        var atiende = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == (int)inicioLimpio.DayOfWeek &&
            horaInicio >= d.HoraInicio &&
            horaFin <= d.HoraFin);

        if (!atiende)
            throw new ArgumentException("El profesional no atiende en el horario o día solicitado.");

        // 3. ¿Hay solapamiento? (Descartando los cancelados)
        var ocupado = await _context.Turnos.AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            t.Estado != "Cancelado" &&
            inicioLimpio < t.FechaHoraFinUtc &&
            t.FechaHoraInicioUtc < finSolicitado);

        if (ocupado)
            throw new ArgumentException("El horario ya se encuentra ocupado.");

        // 4. Instanciamos la entidad de dominio mapeando los datos de invitado (MVP)
        var turno = new Turno
        {
            Id = Guid.CreateVersion7(),

            // Aislamiento Multi-Tenant: El turno pertenece al mismo negocio que el prestador
            NegocioId = prestador.NegocioId,

            PrestadorId = dto.PrestadorId,
            FechaHoraInicioUtc = inicioLimpio,
            FechaHoraFinUtc = finSolicitado,

            // Datos del cliente MVP
            NombreClienteInvitado = dto.NombreClienteInvitado ?? string.Empty,
            TelefonoClienteInvitado = dto.TelefonoClienteInvitado ?? string.Empty,

            Estado = "Pendiente"
        };

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        return _mapper.Map<TurnoReadDto>(turno);
    }
}