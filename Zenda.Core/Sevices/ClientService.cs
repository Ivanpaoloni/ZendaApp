using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;

    public ClienteService(IZendaDbContext context, ITenantService tenantService, IMapper mapper)
    {
        _context = context;
        _tenantService = tenantService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClienteReadDto>> GetAllAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Enumerable.Empty<ClienteReadDto>();

        var clientes = await _context.Clientes
            .AsNoTracking()
            .Where(c => c.NegocioId == negocioId)
            .Include(c => c.Turnos) // Incluimos los turnos para poder contarlos
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        // Mapeamos a mano acá para calcular la CantidadTurnos fácilmente sin tocar AutoMapper
        return clientes.Select(c => new ClienteReadDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Email = c.Email,
            Telefono = c.Telefono,
            Notas = c.Notas,
            CantidadTurnos = c.Turnos.Count
        });
    }

    public async Task<IEnumerable<TurnoReadDto>> GetHistorialTurnosAsync(Guid clienteId)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Enumerable.Empty<TurnoReadDto>();

        // 🎯 BARRERA DE SEGURIDAD: Verificamos que el cliente le pertenezca a este negocio
        var clienteValido = await _context.Clientes
            .AnyAsync(c => c.Id == clienteId && c.NegocioId == negocioId);

        if (!clienteValido)
            throw new UnauthorizedAccessException("El cliente no existe o no pertenece a este negocio.");

        // Traemos los turnos con toda la info necesaria para la UI del Drawer
        var turnos = await _context.Turnos
            .AsNoTracking()
            .Include(t => t.Servicio)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Where(t => t.ClienteId == clienteId && t.NegocioId == negocioId)
            .OrderByDescending(t => t.FechaHoraInicioUtc) // Ordenados del más nuevo al más viejo
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }
}