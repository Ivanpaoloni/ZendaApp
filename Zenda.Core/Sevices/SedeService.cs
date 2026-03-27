using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class SedeService : ISedeService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public SedeService(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SedeReadDto>> GetAllAsync()
    {
        var sedes = await _context.Sedes
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SedeReadDto>>(sedes);
    }
    public async Task<SedeReadDto> CreateAsync(SedeCreateDto dto)
    {
        var sede = _mapper.Map<Sede>(dto);

        // Generamos el ID si no viene (Guid V7 recomendado)
        if (sede.Id == Guid.Empty) sede.Id = Guid.CreateVersion7();

        sede.CreatedAtUtc = DateTime.UtcNow;
        sede.IsDeleted = false;

        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync();

        return _mapper.Map<SedeReadDto>(sede);
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var sede = await _context.Sedes.FindAsync(id);

        if (sede == null) return false;

        // Soft Delete: Solo marcamos como eliminado
        sede.IsDeleted = true;

        return await _context.SaveChangesAsync() > 0;
    }
}