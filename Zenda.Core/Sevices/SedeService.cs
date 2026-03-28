using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

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
        // Usamos IgnoreQueryFilters porque el cliente externo no tiene Token/TenantId
        var sedes = await _context.Sedes
            .IgnoreQueryFilters()
            .Where(s => s.NegocioId == negocioId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SedeReadDto>>(sedes);
    }

    // --- MÉTODOS PRIVADOS (Panel Admin - Usan Filtro Automático) ---

    public async Task<IEnumerable<SedeReadDto>> GetAllAsync()
    {
        var sedes = await _context.Sedes.ToListAsync();
        return _mapper.Map<IEnumerable<SedeReadDto>>(sedes);
    }

    public async Task<SedeReadDto> CreateAsync(SedeCreateDto dto)
    {
        var sede = _mapper.Map<Sede>(dto);
        sede.NegocioId = _tenantService.GetCurrentTenantId() ?? throw new UnauthorizedAccessException();

        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync();
        return _mapper.Map<SedeReadDto>(sede);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Buscamos la sede e incluimos los prestadores para contar
        var sede = await _context.Sedes
            .Include(s => s.Prestadores)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sede == null) return false;

        if (sede.Prestadores.Any())
        {
            // Lanzamos una excepción propia con un mensaje claro
            throw new InvalidOperationException("No se puede eliminar la sede porque tiene profesionales asociados.");
        }

        _context.Sedes.Remove(sede);
        return await _context.SaveChangesAsync() > 0;
    }
}