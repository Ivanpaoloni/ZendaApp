using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class PrestadoresService : IPrestadoresService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public PrestadoresService(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PrestadorReadDto>> GetAllAsync()
    {
        var prestadores = await _context.Prestadores
            .Include(p => p.Horarios)
            .ToListAsync();
        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    // Reemplazamos GetBySlugAsync por GetByIdAsync
    public async Task<PrestadorReadDto?> GetByIdAsync(Guid id)
    {
        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Id == id);

        return prestador == null ? null : _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto)
    {
        var prestador = _mapper.Map<Prestador>(dto);
        //if (prestador.SedeId == Guid.Empty) prestador.SedeId = Guid.Parse("d84bb65e-8ed6-46a8-964c-5250761dad96");
        // Asignación explícita del Guid secuencial en la capa de servicio
        prestador.Id = Guid.CreateVersion7();
        //if (prestador.SedeId == Guid.Empty) prestador.SedeId = null;
        // Validación de seguridad 
        if (prestador.DuracionTurnoMinutos <= 0) prestador.DuracionTurnoMinutos = 30;

        _context.Prestadores.Add(prestador);
        await _context.SaveChangesAsync();

        return _mapper.Map<PrestadorReadDto>(prestador);
    }
    // En PrestadorService.cs
    public async Task<IEnumerable<PrestadorReadDto>> GetBySedeAsync(Guid sedeId)
    {
        return await _context.Prestadores
            .AsNoTracking()
            .Where(p => p.SedeId == sedeId)
            .Select(p => new PrestadorReadDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                // Agregá los campos que tengas en tu DTO
            })
            .ToListAsync();
    }
    public async Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto)
    {
        var prestadorDb = await _context.Prestadores.FindAsync(id);
        if (prestadorDb == null) return false;

        _mapper.Map(dto, prestadorDb);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var prestador = await _context.Prestadores.FindAsync(id);
        if (prestador == null) return false;

        _context.Prestadores.Remove(prestador);
        await _context.SaveChangesAsync();
        return true;
    }
}