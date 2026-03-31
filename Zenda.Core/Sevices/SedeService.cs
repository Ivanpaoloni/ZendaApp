using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class SedeService : ISedeService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public SedeService(IZendaDbContext context, ITenantService tenantService, IMapper mapper)
    {
        _context = context;
        _tenantService = tenantService;
        _mapper = mapper;
    }

    // --- MÉTODOS PÚBLICOS (Para la página de reserva) ---

    public async Task<IEnumerable<SedeReadDto>> GetPublicByNegocioIdAsync(Guid negocioId)
    {
        var sedes = await _context.Sedes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.NegocioId == negocioId)
            // .Where(s => !s.IsDeleted) // Descomentá esto si en el futuro pasás a borrado lógico
            .ToListAsync();

        return _mapper.Map<IEnumerable<SedeReadDto>>(sedes);
    }

    // --- MÉTODOS PRIVADOS (Panel Admin - Usan Token) ---

    public async Task<IEnumerable<SedeReadDto>> GetAllAsync()
    {
        // El QueryFilter automático en ZendaDbContext filtra por el TenantId del usuario
        var sedes = await _context.Sedes.AsNoTracking().ToListAsync();
        return _mapper.Map<IEnumerable<SedeReadDto>>(sedes);
    }

    public async Task<SedeReadDto> CreateAsync(SedeCreateDto dto)
    {
        var sede = _mapper.Map<Sede>(dto);
        sede.NegocioId = _tenantService.GetCurrentTenantId() ?? throw new UnauthorizedAccessException();

        // Es buena práctica forzar el ID si estás usando UUIDv7 para ordenamiento
        sede.Id = Guid.CreateVersion7();

        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync();
        return _mapper.Map<SedeReadDto>(sede);
    }

    // 🎯 NUEVO: Método para editar
    public async Task<bool> UpdateAsync(Guid id, SedeCreateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        // 🛡️ Buscamos exigiendo que la Sede pertenezca al NegocioId actual
        var sedeDb = await _context.Sedes
            .FirstOrDefaultAsync(s => s.Id == id && s.NegocioId == tenantId.Value);

        if (sedeDb == null) return false;

        // Actualizamos los campos
        sedeDb.Nombre = dto.Nombre;
        sedeDb.Direccion = dto.Direccion;
        sedeDb.ZonaHorariaId = dto.ZonaHorariaId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        // 🛡️ Seguridad explícita: Verificamos que intente borrar SU propia sede
        var sede = await _context.Sedes
            .Include(s => s.Prestadores)
            .FirstOrDefaultAsync(s => s.Id == id && s.NegocioId == tenantId.Value);

        if (sede == null) return false;

        if (sede.Prestadores.Any())
        {
            throw new InvalidOperationException("No se puede eliminar la sede porque tiene profesionales asociados.");
        }

        _context.Sedes.Remove(sede);
        return await _context.SaveChangesAsync() > 0;
    }
}