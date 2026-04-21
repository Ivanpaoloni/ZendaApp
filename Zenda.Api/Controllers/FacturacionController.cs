using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Solo usuarios logueados pueden ver esto
public class FacturacionController : ControllerBase
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;

    public FacturacionController(IZendaDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumenFacturacion()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) return Unauthorized();

        // 1. Buscamos el Negocio, su Suscripción activa y su Plan
        var negocio = await _context.Negocios
            .Include(n => n.Sedes)
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        if (negocio == null) return NotFound();

        var suscripcion = await _context.SuscripcionesNegocio
            .Include(s => s.PlanSuscripcion)
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

        // 2. Calculamos los profesionales usados (contando los que pertenecen al negocio)
        var profesionalesUsados = await _context.Prestadores
            //.CountAsync(p => p.NegocioId == negocioId && p.Activo); actualmente no tenemos el campo Activo, así que solo contamos por negocio
            .CountAsync(p => p.NegocioId == negocioId);

        // 3. Buscamos los últimos pagos
        var historial = await _context.HistorialPagos
            .Include(h => h.SuscripcionNegocio)
            .ThenInclude(s => s.PlanSuscripcion)
            .Where(h => h.SuscripcionNegocio.NegocioId == negocioId)
            .OrderByDescending(h => h.FechaPago)
            .Take(10) // Traemos los últimos 10 pagos
            .Select(h => new HistorialPagoDto
            {
                Fecha = h.FechaPago,
                Monto = h.MontoCobrado,
                PlanNombre = h.SuscripcionNegocio.PlanSuscripcion.Nombre,
                TransaccionId = h.MercadoPagoPaymentId
            })
            .ToListAsync();

        // 4. Armamos el DTO de respuesta
        var response = new FacturacionDto
        {
            PlanActualNombre = suscripcion?.PlanSuscripcion?.Nombre ?? "Single",
            Estado = suscripcion?.Estado.ToString() ?? "Activa",
            FechaVencimiento = suscripcion?.FechaVencimiento ?? DateTime.UtcNow.AddYears(1),
            SedesUsadas = negocio.Sedes.Count(),
            SedesMaximas = suscripcion?.PlanSuscripcion?.MaxSedes ?? 1,
            ProfesionalesUsados = profesionalesUsados,
            ProfesionalesMaximos = suscripcion?.PlanSuscripcion?.MaxProfesionales ?? 1,
            Pagos = historial
        };

        return Ok(response);
    }
}