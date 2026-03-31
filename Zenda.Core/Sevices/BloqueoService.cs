using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Sevices
{
    public class BloqueoService : IBloqueoService
    {// En tu servicio de aplicación
        public readonly IMapper _mapper;
        private readonly IZendaDbContext _context;

        public BloqueoService(IMapper mapper, IZendaDbContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto)
        {
            // 🛡️ Validación: No permitir que el fin sea antes que el inicio
            if (dto.FinLocal <= dto.InicioLocal) return false;

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
