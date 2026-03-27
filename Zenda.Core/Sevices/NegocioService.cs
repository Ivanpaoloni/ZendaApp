using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

public class NegocioService : INegocioService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public NegocioService(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<NegocioReadDto?> GetByIdAsync(Guid id)
    {
        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        return _mapper.Map<NegocioReadDto>(negocio);
    }

    public async Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto)
    {
        var negocio = _mapper.Map<Negocio>(dto);
        negocio.Id = Guid.NewGuid(); // O GuidV7
        negocio.Slug = dto.Nombre.ToLower().Replace(" ", "-");
        negocio.CreatedAtUtc = DateTime.UtcNow;

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return _mapper.Map<NegocioReadDto>(negocio);
    }
}