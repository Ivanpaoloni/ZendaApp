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

        // Normalizamos la fecha a UTC para el filtro de base de datos
        int diaBuscado = (int)fecha.DayOfWeek;
        var fechaFiltroInicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
        var fechaFiltroFin = fechaFiltroInicio.AddDays(1);

        var configuracion = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId &&
                        t.FechaHoraInicioUtc >= fechaFiltroInicio &&
                        t.FechaHoraInicioUtc < fechaFiltroFin &&
                        t.Estado != "Cancelado")
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

                // Verificamos si este slot choca con algún turno ocupado
                // Convertimos el UTC de la DB a Local para comparar "Horas Murales"
                bool estaOcupado = turnosOcupados.Any(t =>
                {
                    var hInicioOcupado = TimeOnly.FromDateTime(t.FechaHoraInicioUtc.ToLocalTime());
                    var hFinOcupado = TimeOnly.FromDateTime(t.FechaHoraFinUtc.ToLocalTime());

                    return hInicioOcupado < finSlot && inicioSlot < hFinOcupado;
                });

                if (!estaOcupado)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                inicioSlot = finSlot;

                // Seguridad ante configuraciones de 0 minutos o errores
                if (duracion <= 0) break;
            }
        }

        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        // 1. Buscamos al prestador INCLUYENDO su sede para saber dónde trabaja
        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador?.Sede == null)
            throw new Exception("Prestador o Sede no encontrados.");

        // 2. Obtenemos la zona horaria real del local
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // 3. Nos aseguramos de que la fecha entrante no tenga rastros de UTC
        var fechaCruda = DateTime.SpecifyKind(dto.Inicio, DateTimeKind.Unspecified);

        // 4. LA MAGIA: Convertimos esas 09:00 crudas a UTC basados en la Sede
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaCruda, zonaSede);

        // 5. Armamos la entidad
        var nuevoTurno = new Turno
        {
            NegocioId = prestador.NegocioId,
            PrestadorId = dto.PrestadorId,
            FechaHoraInicioUtc = fechaUtcDefinitiva, // ¡Perfecto y a prueba de balas!
            FechaHoraFinUtc = fechaUtcDefinitiva.AddMinutes(prestador.DuracionTurnoMinutos),
            NombreClienteInvitado = dto.NombreClienteInvitado,
            TelefonoClienteInvitado = dto.TelefonoClienteInvitado,
            EmailClienteInvitado = dto.EmailClienteInvitado,
            Estado = "Pendiente"
        };

        _context.Turnos.Add(nuevoTurno);
        await _context.SaveChangesAsync();

        return _mapper.Map<TurnoReadDto>(nuevoTurno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha)
    {
        // 1. Extraemos solo la fecha (00:00:00) y la forzamos explícitamente a UTC
        var fechaInicio = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);

        // 2. Sumamos un día exacto
        var fechaFin = fechaInicio.AddDays(1);

        return await _context.Turnos
            .AsNoTracking()
            .Where(t => t.FechaHoraInicioUtc >= fechaInicio && t.FechaHoraInicioUtc < fechaFin)
            .OrderBy(t => t.FechaHoraInicioUtc)
            .Select(t => new TurnoReadDto
            {
                Id = t.Id,
                FechaHoraInicioUtc = t.FechaHoraInicioUtc,
                FechaHoraFinUtc = t.FechaHoraFinUtc,
                NombreClienteInvitado = t.NombreClienteInvitado,
                TelefonoClienteInvitado = t.TelefonoClienteInvitado,
                EmailClienteInvitado = t.EmailClienteInvitado,
                Estado = t.Estado
            })
            .ToListAsync();
    }
}