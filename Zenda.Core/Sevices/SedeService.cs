using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
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
}