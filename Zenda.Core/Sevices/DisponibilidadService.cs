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
}