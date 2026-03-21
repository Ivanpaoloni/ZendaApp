using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

public class TurnosService : ITurnosService
{
    private readonly IZendaDbContext _context; // <--- Usamos la Interfaz
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
        // 1. Buscamos al prestador para saber SU duración de turno
        var prestador = await _context.Prestadores
            .Select(p => new { p.Id, p.DuracionTurnoMinutos })
            .FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null) throw new Exception("Prestador no encontrado");

        int duracion = prestador.DuracionTurnoMinutos;
        int diaBuscado = (int)fecha.DayOfWeek;

        // Normalizamos la fecha a UTC para la consulta en Postgres
        var fechaFiltro = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);

        // 2. Traemos la configuración de horas de atención y los turnos ya reservados
        var configuracion = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId && d.DiaSemana == diaBuscado)
            .ToListAsync();

        var turnosOcupados = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId && t.Inicio.Date == fechaFiltro)
            .ToListAsync();

        var respuesta = new DisponibilidadFechaDto { Fecha = fechaFiltro };

        // 3. Generador de Slots Dinámico
        foreach (var rango in configuracion)
        {
            var inicioSlot = rango.HoraInicio;

            while (inicioSlot.AddMinutes(duracion) <= rango.HoraFin)
            {
                var finSlot = inicioSlot.AddMinutes(duracion);

                // Verificamos si este bloque choca con algún turno existente
                bool estaOcupado = turnosOcupados.Any(t =>
                    TimeOnly.FromDateTime(t.Inicio) < finSlot &&
                    inicioSlot < TimeOnly.FromDateTime(t.Fin));

                if (!estaOcupado)
                {
                    respuesta.HorariosLibres.Add(inicioSlot.ToString("HH:mm"));
                }

                inicioSlot = finSlot; // Avanzamos a la siguiente posición
            }
        }

        return respuesta;
    }

    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        // Usamos FindAsync a través de la interfaz
        var prestador = await _context.FindAsync<Prestador>(dto.PrestadorId);
        if (prestador == null) throw new Exception("Prestador no encontrado");

        var inicioLimpio = new DateTime(
            dto.Inicio.Year, dto.Inicio.Month, dto.Inicio.Day,
            dto.Inicio.Hour, dto.Inicio.Minute, 0, DateTimeKind.Utc);

        var finSolicitado = inicioLimpio.AddMinutes(prestador.DuracionTurnoMinutos);

        // Validaciones (atiende / ocupado) usando _context.Disponibilidad y _context.Turnos...
        // ... (misma lógica que antes) ...

        var turno = _mapper.Map<Turno>(dto);
        turno.Id = Guid.NewGuid();
        turno.Inicio = inicioLimpio;
        turno.Fin = finSolicitado;

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        return _mapper.Map<TurnoReadDto>(turno);
    }

    // ... Implementación de los otros métodos del Service ...
}