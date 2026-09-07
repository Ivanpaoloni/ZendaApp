using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Sevices
{
    public class BloqueoService : IBloqueoService
    {
        public readonly IMapper _mapper;
        private readonly IZendaDbContext _context;

        public BloqueoService(IMapper mapper, IZendaDbContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto)
        {
            if (dto.FinLocal <= dto.InicioLocal) 
                return false;

            var bloqueo = _mapper.Map<BloqueoAgenda>(dto);
            _context.BloqueosAgenda.Add(bloqueo);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<BloqueoReadDto>> GetBloqueosFuturos(Guid prestadorId)
        {
            var bloqueos = await _context.BloqueosAgenda
                .Where(b => b.PrestadorId == prestadorId && b.FinUtc >= DateTime.UtcNow)
                .OrderBy(b => b.InicioUtc)
                .ToListAsync();

            return _mapper.Map<List<BloqueoReadDto>>(bloqueos);
        }
    }
}
