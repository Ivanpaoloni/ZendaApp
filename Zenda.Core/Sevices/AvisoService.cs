using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Services
{
    public class AvisoService : IAvisoService
    {
        private readonly IZendaDbContext _context;
        private readonly IMapper _mapper;

        public AvisoService(IZendaDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<AvisoDto?> GetAvisoActivoAsync()
        {
            var aviso = await _context.Avisos
                .AsNoTracking()
                .Where(a => !a.IsDeleted && a.Activo)
                .FirstOrDefaultAsync();

            return aviso == null ? null : _mapper.Map<AvisoDto>(aviso);
        }

        public async Task<IEnumerable<AvisoDto>> GetAllAsync()
        {
            var avisos = await _context.Avisos
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AvisoDto>>(avisos);
        }

        public async Task<AvisoDto> CreateAsync(AvisoDto avisoDto)
        {
            var aviso = _mapper.Map<Aviso>(avisoDto);

            // Por defecto, un aviso nuevo nace inactivo para poder revisarlo antes de publicarlo
            aviso.Activo = false;

            _context.Avisos.Add(aviso);
            await _context.SaveChangesAsync();

            return _mapper.Map<AvisoDto>(aviso);
        }

        public async Task<AvisoDto> UpdateAsync(Guid id, AvisoDto avisoDto)
        {
            var aviso = await _context.Avisos.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (aviso == null) throw new Exception("Aviso no encontrado");

            aviso.Titulo = avisoDto.Titulo;
            aviso.ContenidoHtml = avisoDto.ContenidoHtml;
            aviso.ImageUrl = avisoDto.ImageUrl;

            // No actualizamos 'Activo' por este método para forzar el uso de ActivarAvisoAsync

            await _context.SaveChangesAsync();
            return _mapper.Map<AvisoDto>(aviso);
        }

        public async Task DeleteAsync(Guid id)
        {
            var aviso = await _context.Avisos.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (aviso != null)
            {
                aviso.IsDeleted = true; // Soft Delete
                if (aviso.Activo) aviso.Activo = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ActivarAvisoAsync(Guid id)
        {
            // 1. Buscamos el aviso que queremos activar
            var avisoAActivar = await _context.Avisos.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (avisoAActivar == null) throw new Exception("Aviso no encontrado");

            // 2. Buscamos si hay algún otro aviso activo actualmente y lo desactivamos
            var avisosActivos = await _context.Avisos.Where(a => a.Activo && a.Id != id).ToListAsync();
            foreach (var activo in avisosActivos)
            {
                activo.Activo = false;
            }

            // 3. Activamos el nuevo
            avisoAActivar.Activo = true;

            await _context.SaveChangesAsync();
        }
    }
}