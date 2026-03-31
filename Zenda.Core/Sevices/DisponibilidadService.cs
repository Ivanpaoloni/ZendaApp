using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

public class DisponibilidadService : IDisponibilidadService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public DisponibilidadService(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DisponibilidadReadDto>> GetByPrestadorAsync(Guid prestadorId)
    {
        var horarios = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DisponibilidadReadDto>>(horarios);
    }

    public async Task<DisponibilidadReadDto> CreateAsync(DisponibilidadCreateDto dto)
    {
        // 1. Validaciones de Negocio
        if (dto.HoraInicio >= dto.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");

        var prestador = await _context.Prestadores.AnyAsync(p => p.Id == dto.PrestadorId);
        if (!prestador) throw new ArgumentException("El prestador no existe.");

        // 2. Validación de Solapamiento (Overlap)
        bool haySolapamiento = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == dto.DiaSemana &&
            dto.HoraInicio < d.HoraFin && d.HoraInicio < dto.HoraFin);

        if (haySolapamiento) throw new ArgumentException("El horario se superpone con uno existente.");

        // 3. Mapeo y Guardado
        var disponibilidad = _mapper.Map<Disponibilidad>(dto);
        disponibilidad.Id = Guid.CreateVersion7();

        _context.Disponibilidad.Add(disponibilidad);
        await _context.SaveChangesAsync();

        return _mapper.Map<DisponibilidadReadDto>(disponibilidad);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var disp = await _context.Disponibilidad.FirstOrDefaultAsync(x => x.Id == id);
        if (disp == null) return false;

        _context.Disponibilidad.Remove(disp);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpsertAgendaAsync(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda)
    {
        // Verificamos existencia
        var existePrestador = await _context.Prestadores.AnyAsync(p => p.Id == prestadorId);
        if (!existePrestador) return false;

        // Limpieza de agenda anterior
        var actual = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .ToListAsync();

        _context.Disponibilidad.RemoveRange(actual);

        // Mapeo masivo e inserción
        var nuevasDisponibilidades = agenda.Select(item => {
            var d = _mapper.Map<Disponibilidad>(item);
            d.Id = Guid.NewGuid();
            d.PrestadorId = prestadorId;
            return d;
        }).ToList();

        _context.Disponibilidad.AddRange(nuevasDisponibilidades);

        return await _context.SaveChangesAsync() > 0;
    }
    
    public async Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto)
    {
        if (dto.FinLocal <= dto.InicioLocal)
            throw new ArgumentException("El fin debe ser posterior al inicio.");

        // 1. Buscamos la sede para saber su zona horaria y convertir a UTC
        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador == null || prestador.Sede == null)
            throw new ArgumentException("Prestador o Sede inválidos.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // Convertimos la hora local que eligió el barbero a UTC para guardar en DB
        var inicioCrudo = DateTime.SpecifyKind(dto.InicioLocal, DateTimeKind.Unspecified);
        var finCrudo = DateTime.SpecifyKind(dto.FinLocal, DateTimeKind.Unspecified);

        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioCrudo, zonaSede);
        var finUtc = TimeZoneInfo.ConvertTimeToUtc(finCrudo, zonaSede);

        var bloqueo = new BloqueoAgenda
        {
            Id = Guid.CreateVersion7(),
            PrestadorId = dto.PrestadorId,
            SedeId = dto.SedeId,
            InicioUtc = inicioUtc,
            FinUtc = finUtc,
            Motivo = dto.Motivo
        };

        _context.BloqueosAgenda.Add(bloqueo);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<BloqueoReadDto>> GetBloqueosFuturosAsync(Guid prestadorId)
    {
        var prestador = await _context.Prestadores.Include(p => p.Sede).FirstOrDefaultAsync(p => p.Id == prestadorId);
        if (prestador == null) return new List<BloqueoReadDto>();

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        var bloqueos = await _context.BloqueosAgenda
            .Where(b => b.PrestadorId == prestadorId && b.FinUtc >= DateTime.UtcNow)
            .OrderBy(b => b.InicioUtc)
            .ToListAsync();

        return bloqueos.Select(b => new BloqueoReadDto
        {
            Id = b.Id,
            PrestadorId = b.PrestadorId,
            SedeId = b.SedeId,
            Motivo = b.Motivo,
            // Devolvemos en hora local para que el Frontend lo muestre bien
            InicioLocal = TimeZoneInfo.ConvertTimeFromUtc(b.InicioUtc, zonaSede),
            FinLocal = TimeZoneInfo.ConvertTimeFromUtc(b.FinUtc, zonaSede)
        });
    }

    public async Task<bool> EliminarBloqueoAsync(Guid id)
    {
        var bloqueo = await _context.BloqueosAgenda.FindAsync(id);
        if (bloqueo == null) return false;

        _context.BloqueosAgenda.Remove(bloqueo);
        return await _context.SaveChangesAsync() > 0;
    }
}