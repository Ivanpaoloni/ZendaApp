using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

public class NegocioService : INegocioService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantService _tenantService;

    public NegocioService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // 🔥 EL FIX: En lugar de tirar un throw, simplemente devolvemos null.
        // Esto es mucho más seguro y evita que el backend explote.
        if (tenantId == null)
            return null;

        var negocio = await _context.Negocios
            .Include(n => n.Sedes)
            .Include(n => n.PlanSuscripcion)
            .FirstOrDefaultAsync(n => n.Id == tenantId);

        // Acá también, si no existe, devolvemos null en vez de explotar
        if (negocio == null)
            return null;

        if (!negocio.IsActive)
        {
            throw new UnauthorizedAccessException("CUENTA_SUSPENDIDA");
        }

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        if (negocio.PlanSuscripcion != null)
        {
            dto.PlanNombre = negocio.PlanSuscripcion.Nombre;
            dto.PlanSuscripcionId = negocio.PlanSuscripcionId;
            dto.MaxProfesionales = negocio.PlanSuscripcion.MaxProfesionales;
            dto.MaxSedes = negocio.PlanSuscripcion.MaxSedes;
        }

        return dto;
    }
    public async Task<NegocioReadDto?> GetPublicBySlugAsync(string slug)
    {
        // 1. Consulta pública del negocio
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == slug);

        if (negocio == null) return null;

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        // 2. 🎯 MAGIA DE IDENTITY: Usamos _context.Users (disponible gracias a IdentityDbContext)
        var usuarioDueño = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NegocioId == negocio.Id);

        if (usuarioDueño != null)
        {
            // Identity usa "PhoneNumber" de forma nativa
            dto.Telefono = usuarioDueño.PhoneNumber;
        }

        return dto;
    }

    public async Task<NegocioReadDto?> GetByIdAsync(Guid id)
    {
        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        return _mapper.Map<NegocioReadDto>(negocio);
    }

    public async Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto)
    {
        var negocio = _mapper.Map<Negocio>(dto);
        negocio.Id = Guid.CreateVersion7(); // O GuidV7
        negocio.Slug = dto.Slug.ToLower().Replace(" ", "-");
        negocio.CreatedAtUtc = DateTime.UtcNow;

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return _mapper.Map<NegocioReadDto>(negocio);
    }
    public async Task<bool> IsSlugAvailableAsync(string slug)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // 🛡️ REGLA DE ORO: Ignoramos los filtros porque los slugs son únicos globales.
        // Pero excluimos MI PROPIO negocio, porque si el slug es el que ya tengo, ¡obvio que está disponible para mí!
        var slugOcupado = await _context.Negocios
            .IgnoreQueryFilters()
            .AnyAsync(n => n.Slug.ToLower() == slug.ToLower() && n.Id != tenantId);

        return !slugOcupado; // Si NO está ocupado, está disponible (true)
    }

    public async Task<bool> UpdatePerfilAsync(NegocioUpdateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == tenantId);
        if (negocio == null) return false;

        // Doble validación por seguridad (por si alguien saltea el frontend)
        if (!await IsSlugAvailableAsync(dto.Slug))
            throw new InvalidOperationException("El link elegido ya está en uso por otra empresa.");

        negocio.Nombre = dto.Nombre;
        negocio.IntervaloTurnosMinutos = dto.IntervaloTurnosMinutos;
        negocio.AnticipacionMinimaHoras = dto.AnticipacionMinimaHoras;
        negocio.RubroId = dto.RubroId;
        // Limpiamos el slug por si nos mandan mayúsculas o espacios raros
        negocio.Slug = dto.Slug.ToLower().Replace(" ", "-").Trim();

        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateLogoUrlAsync(string logoUrl)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == tenantId);
        if (negocio == null) return false;

        negocio.LogoUrl = logoUrl;
        await _context.SaveChangesAsync();

        return true;
    }
}