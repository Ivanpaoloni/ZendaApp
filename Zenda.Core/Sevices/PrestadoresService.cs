using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class PrestadoresService : IPrestadoresService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public PrestadoresService(IZendaDbContext context, ITenantService tenantService, IMapper mapper)
    {
        _context = context;
        _tenantService = tenantService;
        _mapper = mapper;
    }

    #region MÉTODOS PÚBLICOS (Para la página de reserva - Sin Token)

    public async Task<IEnumerable<PrestadorReadDto>> GetPublicBySedeIdAsync(Guid sedeId)
    {
        var prestadores = await _context.Prestadores
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Servicios) //  NUEVO: Traemos qué servicios hace cada uno
            .Where(p => p.SedeId == sedeId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    #endregion

    #region MÉTODOS ADMINISTRATIVOS (Para el Panel - Requieren Token)

    public async Task<IEnumerable<PrestadorReadDto>> GetAllAsync()
    {
        var prestadores = await _context.Prestadores
            .Include(p => p.Horarios)
            .Include(p => p.Servicios) //  NUEVO: Para ver las habilidades en el dashboard
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    public async Task<PrestadorReadDto?> GetByIdAsync(Guid id)
    {
        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .Include(p => p.Servicios) //  NUEVO: Necesario para la vista de edición
            .FirstOrDefaultAsync(p => p.Id == id);

        return prestador == null ? null : _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto)
    {
        var prestador = _mapper.Map<Prestador>(dto);

        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) throw new UnauthorizedAccessException("Contexto de negocio no identificado.");

        prestador.NegocioId = tenantId.Value;
        prestador.Id = Guid.CreateVersion7();

        //  NUEVO: Asignación de Habilidades (Servicios)
        if (dto.ServiciosIds != null && dto.ServiciosIds.Any())
        {
            // Buscamos los servicios reales en la BD asegurándonos que sean de este negocio
            var serviciosAsignados = await _context.Servicios
                .Where(s => s.NegocioId == tenantId.Value && dto.ServiciosIds.Contains(s.Id))
                .ToListAsync();

            prestador.Servicios = serviciosAsignados;
        }

        if (prestador.DuracionTurnoMinutos <= 0) prestador.DuracionTurnoMinutos = 30;

        _context.Prestadores.Add(prestador);
        await _context.SaveChangesAsync();

        return _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var prestadorDb = await _context.Prestadores
            .Include(p => p.Horarios)
            .Include(p => p.Servicios) //  NUEVO: Fundamental hacer el Include para actualizar la relación M2M
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prestadorDb == null) return false;

        _mapper.Map(dto, prestadorDb);

        // NUEVO: Actualización de Habilidades (Servicios)
        if (dto.ServiciosIds != null)
        {
            // 1. Limpiamos las relaciones actuales (EF Core detecta esto y borra en la tabla intermedia)
            prestadorDb.Servicios.Clear();

            // 2. Si mandó nuevos IDs, los buscamos y los agregamos
            if (dto.ServiciosIds.Any())
            {
                var nuevosServicios = await _context.Servicios
                    .Where(s => s.NegocioId == tenantId.Value && dto.ServiciosIds.Contains(s.Id))
                    .ToListAsync();

                foreach (var servicio in nuevosServicios)
                {
                    prestadorDb.Servicios.Add(servicio);
                }
            }
        }

        if (prestadorDb.DuracionTurnoMinutos <= 0) prestadorDb.DuracionTurnoMinutos = 30;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var prestador = await _context.Prestadores.FirstOrDefaultAsync(p => p.Id == id);
        if (prestador == null) return false;

        _context.Prestadores.Remove(prestador);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    #endregion
}