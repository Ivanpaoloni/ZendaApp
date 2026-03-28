using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

public class NegocioService : INegocioService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantService _tenantService; // 👈 1. Inyectamos esto

    public NegocioService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        // 2. Le preguntamos al token "che, ¿de qué negocio es este usuario?"
        var tenantId = _tenantService.GetCurrentTenantId();

        if (tenantId == null)
            throw new UnauthorizedAccessException("El usuario no tiene un negocio asignado.");

        // 3. Buscamos EXACTAMENTE ese ID en la base de datos
        var negocio = await _context.Negocios
            .Include(n => n.Sedes)
            .FirstOrDefaultAsync(n => n.Id == tenantId);

        return _mapper.Map<NegocioReadDto>(negocio);
    }
    public async Task<NegocioReadDto?> GetPublicBySlugAsync(string slug)
    {
        // consulta pública
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == slug);

        return _mapper.Map<NegocioReadDto>(negocio);
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