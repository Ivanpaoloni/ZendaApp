using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class ServicioService : IServicioService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public ServicioService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<CategoriaServicioReadDto>> GetCatalogoAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var categorias = await _context.CategoriasServicio
            .Where(c => c.NegocioId == negocioId)
            .Include(c => c.Servicios.Where(s => s.Activo))
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoriaServicioReadDto>>(categorias);
    }

    public async Task<CategoriaServicioReadDto> CreateCategoriaAsync(CategoriaServicioCreateDto dto)
    {
        var nueva = new CategoriaServicio
        {
            Id = Guid.NewGuid(),
            NegocioId = _tenantService.GetCurrentTenantId()!.Value,
            Nombre = dto.Nombre
        };

        _context.CategoriasServicio.Add(nueva);
        await _context.SaveChangesAsync();
        return _mapper.Map<CategoriaServicioReadDto>(nueva);
    }

    public async Task<ServicioReadDto> CreateServicioAsync(ServicioCreateDto dto)
    {
        var nuevo = new Servicio
        {
            Id = Guid.NewGuid(),
            NegocioId = _tenantService.GetCurrentTenantId()!.Value,
            CategoriaId = dto.CategoriaId,
            Nombre = dto.Nombre,
            DuracionMinutos = dto.DuracionMinutos,
            Precio = dto.Precio,
            Activo = true
        };

        _context.Servicios.Add(nuevo);
        await _context.SaveChangesAsync();
        return _mapper.Map<ServicioReadDto>(nuevo);
    }

    public async Task<IEnumerable<ServicioReadDto>> GetServiciosPublicosBySedeAsync(Guid sedeId)
    {
        // En este método público usaríamos IgnoreQueryFilters() 
        // pero filtraríamos por el NegocioId de la Sede para seguridad
        var sede = await _context.Sedes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sedeId);

        if (sede == null) return Enumerable.Empty<ServicioReadDto>();

        var servicios = await _context.Servicios
            .IgnoreQueryFilters()
            .Where(s => s.NegocioId == sede.NegocioId && s.Activo)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ServicioReadDto>>(servicios);
    }

    public async Task<IEnumerable<ServicioPublicoDto>> GetServiciosPublicosPorSedeAsync(Guid sedeId)
    {
        // 1. Buscamos a qué negocio pertenece esta sede (ignorando filtros porque el cliente no está logueado)
        var sede = await _context.Sedes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sedeId);

        if (sede == null) return Enumerable.Empty<ServicioPublicoDto>();

        // 2. Traemos los servicios activos de ese negocio y los proyectamos al DTO plano
        var servicios = await _context.Servicios
            .IgnoreQueryFilters()
            .Include(s => s.Categoria) // Incluimos la categoría para sacar el nombre
            .Where(s => s.NegocioId == sede.NegocioId && s.Activo)
            .Select(s => new ServicioPublicoDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                DuracionMinutos = s.DuracionMinutos,
                Precio = s.Precio,
                CategoriaNombre = s.Categoria.Nombre
            })
            .ToListAsync();

        return servicios;
    }
}