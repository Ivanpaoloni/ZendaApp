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
    private readonly IPlanService _planService;

    public PrestadoresService(IZendaDbContext context, ITenantService tenantService, IMapper mapper, IPlanService planService)
    {
        _context = context;
        _tenantService = tenantService;
        _mapper = mapper;
        _planService = planService;
    }

    #region MÉTODOS PÚBLICOS (Para la página de reserva - Sin Token)

    public async Task<IEnumerable<PrestadorReadDto>> GetPublicBySedeIdAsync(Guid sedeId)
    {
        var prestadores = await _context.Prestadores
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Servicios)
            .Where(p => p.SedeId == sedeId && !p.IsDeleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    #endregion

    #region MÉTODOS ADMINISTRATIVOS (Para el Panel - Requieren Token)

    public async Task<IEnumerable<PrestadorReadDto>> GetAllAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Enumerable.Empty<PrestadorReadDto>();

        var prestadores = await _context.Prestadores
            .Where(p => p.NegocioId == negocioId && !p.IsDeleted)
            .Include(p => p.Horarios)
            .Include(p => p.Servicios)
            .Include(p => p.Sede)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    public async Task<PrestadorReadDto?> GetByIdAsync(Guid id)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return null;

        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .Include(p => p.Servicios)
            .Include(p => p.Sede)
            .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == negocioId && !p.IsDeleted);

        return prestador == null ? null : _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto)
    {
        // Validamos el límite del plan antes de hacer nada
        if (!await _planService.PuedeAgregarProfesionalAsync())
        {
            throw new Exception("Has alcanzado el límite de profesionales de tu plan actual.");
        }
        var prestador = _mapper.Map<Prestador>(dto);

        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) throw new UnauthorizedAccessException("Contexto de negocio no identificado.");

        prestador.NegocioId = tenantId.Value;
        prestador.Id = Guid.CreateVersion7();

        if (dto.ServiciosIds != null && dto.ServiciosIds.Any())
        {
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
            .Include(p => p.Servicios)
            // 🎯 FIX: Seguridad. Aseguramos que solo pueda editar si es su negocio y no está borrado.
            .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == tenantId && !p.IsDeleted);

        if (prestadorDb == null) return false;

        _mapper.Map(dto, prestadorDb);

        if (dto.ServiciosIds != null)
        {
            prestadorDb.Servicios.Clear();

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
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        // 🎯 FIX: Verificamos pertenencia antes de aplicar la baja lógica
        var prestador = await _context.Prestadores
            .FirstOrDefaultAsync(p => p.Id == id && p.NegocioId == tenantId);

        if (prestador == null) return false;

        prestador.IsDeleted = true;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion
}