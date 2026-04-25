using AutoMapper;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;
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
            .Include(c => c.Turnos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return clientes.Select(c => new ClienteReadDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Email = c.Email,
            Telefono = c.Telefono,
            Notas = c.Notas,
            CantidadTurnos = c.Turnos.Count(t => t.Estado == EstadoTurnoEnum.Completado || t.Estado == EstadoTurnoEnum.Confirmado)
        });
    }

    public async Task<IEnumerable<TurnoReadDto>> GetHistorialTurnosAsync(Guid clienteId)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Enumerable.Empty<TurnoReadDto>();

        // BARRERA DE SEGURIDAD: Verificamos que el cliente le pertenezca a este negocio
        var clienteValido = await _context.Clientes
            .AnyAsync(c => c.Id == clienteId && c.NegocioId == negocioId);

        if (!clienteValido)
            throw new UnauthorizedAccessException("El cliente no existe o no pertenece a este negocio.");

        var turnos = await _context.Turnos
            .AsNoTracking()
            .Include(t => t.Servicio)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Where(t => t.ClienteId == clienteId && t.NegocioId == negocioId)
            .OrderByDescending(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<byte[]> GenerarReporteExcelAsync()
    {
        // 1. Obtenemos los clientes usando el método que ya tenés, 
        // que además ya tiene la lógica de seguridad del Tenant.
        var clientes = await GetAllAsync();

        // 2. Armamos el Excel en memoria
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes");

        // Estilos de la cabecera
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Cell(1, 1).Value = "Nombre";
        worksheet.Cell(1, 2).Value = "Teléfono";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Total Reservas";

        // Llenamos los datos
        var currentRow = 2;
        foreach (var cliente in clientes)
        {
            worksheet.Cell(currentRow, 1).Value = cliente.Nombre;
            worksheet.Cell(currentRow, 2).Value = cliente.Telefono ?? "";
            worksheet.Cell(currentRow, 3).Value = cliente.Email ?? "";
            worksheet.Cell(currentRow, 4).Value = cliente.CantidadTurnos;
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        // 3. Convertimos a Stream y devolvemos los bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

}