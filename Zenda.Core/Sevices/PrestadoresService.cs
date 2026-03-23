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

    public async Task<PrestadorReadDto?> GetBySlugAsync(string slug)
    {
        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug.ToLower());

        return prestador == null ? null : _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto)
    {
        var prestador = _mapper.Map<Prestador>(dto);
        prestador.Id = Guid.NewGuid();

        // Validación de seguridad para evitar bucles infinitos
        if (prestador.DuracionTurnoMinutos <= 0) prestador.DuracionTurnoMinutos = 30;

        _context.Prestadores.Add(prestador);
        await _context.SaveChangesAsync();

        return _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto)
    {
        var prestadorDb = await _context.FindAsync<Prestador>(id);
        if (prestadorDb == null) return false;

        _mapper.Map(dto, prestadorDb);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var prestador = await _context.FindAsync<Prestador>(id);
        if (prestador == null) return false;

        _context.Prestadores.Remove(prestador);
        await _context.SaveChangesAsync();
        return true;
    }
}