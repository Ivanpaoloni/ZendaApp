using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

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
        // 1. Validar que el prestador existe
        var prestadorExiste = await _context.Prestadores.AnyAsync(p => p.Id == dto.PrestadorId);
        if (!prestadorExiste) throw new ArgumentException("El prestador no existe.");

        // 2. Validar que el rango sea lógico
        if (dto.HoraInicio >= dto.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");

        // 3. Validar superposición (Overlap)
        // Como ahora dto.HoraInicio es TimeOnly, la comparación funciona directo
        var haySolapamiento = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == dto.DiaSemana &&
            dto.HoraInicio < d.HoraFin && d.HoraInicio < dto.HoraFin);

        if (haySolapamiento) throw new ArgumentException("El horario se superpone con una disponibilidad existente.");

        // 4. Mapeo y Guardado
        var disponibilidad = _mapper.Map<Disponibilidad>(dto);
        disponibilidad.Id = Guid.NewGuid();

        _context.Disponibilidad.Add(disponibilidad);
        await _context.SaveChangesAsync();

        return _mapper.Map<DisponibilidadReadDto>(disponibilidad);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var disp = await _context.FindAsync<Disponibilidad>(id);
        if (disp == null) return false;

        _context.Disponibilidad.Remove(disp);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpsertAgendaAsync(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda)
    {
        var prestador = await _context.FindAsync<Prestador>(prestadorId);
        if (prestador == null) return false;

        // Limpieza y carga atómica
        var actual = await _context.Disponibilidad.Where(d => d.PrestadorId == prestadorId).ToListAsync();
        _context.Disponibilidad.RemoveRange(actual);

        foreach (var item in agenda)
        {
            var disp = _mapper.Map<Disponibilidad>(item);
            disp.Id = Guid.NewGuid();
            disp.PrestadorId = prestadorId;
            _context.Disponibilidad.Add(disp);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}