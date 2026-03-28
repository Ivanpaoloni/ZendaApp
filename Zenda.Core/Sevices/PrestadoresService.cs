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
        // Usamos IgnoreQueryFilters porque el cliente anónimo no tiene TenantId en el token.
        // Filtramos explícitamente por SedeId para mantener el aislamiento.
        var prestadores = await _context.Prestadores
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.SedeId == sedeId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    #endregion

    #region MÉTODOS ADMINISTRATIVOS (Para el Panel - Requieren Token)

    public async Task<IEnumerable<PrestadorReadDto>> GetAllAsync()
    {
        // El Global Query Filter del DbContext se encarga de filtrar por NegocioId.
        var prestadores = await _context.Prestadores
            .Include(p => p.Horarios)
            .AsNoTracking()
            .ToListAsync();

        return _mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores);
    }

    public async Task<PrestadorReadDto?> GetByIdAsync(Guid id)
    {
        // El filtro global impide que encuentres prestadores de otros negocios.
        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Id == id);

        return prestador == null ? null : _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<PrestadorReadDto> CreateAsync(PrestadorCreateDto dto)
    {
        var prestador = _mapper.Map<Prestador>(dto);

        // 🛡️ SEGURIDAD: Obtenemos el TenantId desde el Token (a través de ITenantService)
        // Si no hay tenant (usuario no logueado), lanzamos excepción.
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) throw new UnauthorizedAccessException("Contexto de negocio no identificado.");

        prestador.NegocioId = tenantId.Value;
        prestador.Id = Guid.CreateVersion7(); // Usamos v7 para mejor performance en DB

        // Validación de negocio por defecto
        if (prestador.DuracionTurnoMinutos <= 0) prestador.DuracionTurnoMinutos = 30;

        _context.Prestadores.Add(prestador);
        await _context.SaveChangesAsync();

        return _mapper.Map<PrestadorReadDto>(prestador);
    }

    public async Task<bool> UpdateAsync(Guid id, PrestadorUpdateDto dto)
    {
        // Buscamos al prestador. Si el ID no pertenece a este NegocioId, 
        // el Global Query Filter hará que 'prestadorDb' sea NULL.
        var prestadorDb = await _context.Prestadores
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prestadorDb == null) return false;

        // Mapeamos los cambios del DTO a la Entidad de la DB
        _mapper.Map(dto, prestadorDb);

        // Validaciones extra de negocio si hicieran falta
        if (prestadorDb.DuracionTurnoMinutos <= 0) prestadorDb.DuracionTurnoMinutos = 30;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Si el ID pertenece a otro negocio, FirstOrDefaultAsync devolverá null por el filtro global.
        var prestador = await _context.Prestadores.FirstOrDefaultAsync(p => p.Id == id);
        if (prestador == null) return false;

        _context.Prestadores.Remove(prestador);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    #endregion
}