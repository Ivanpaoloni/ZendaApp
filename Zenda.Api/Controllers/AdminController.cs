using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs.Admin;
using Zenda.Core.Interfaces; // Donde esté tu IZendaDbContext

namespace Zenda.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SuperAdmin")] // ¡Seguridad máxima!
public class AdminController : ControllerBase
{
    private readonly IZendaDbContext _context;

    public AdminController(IZendaDbContext context)
    {
        _context = context;
    }

    // 1. Modificá la consulta del GET negocios para incluir el PlanSuscripcionId
    [HttpGet("negocios")]
    public async Task<IActionResult> GetNegociosAdmin()
    {
        var lista = await _context.SuscripcionesNegocio
            .Include(s => s.Negocio)
            .Include(s => s.PlanSuscripcion)
            .IgnoreQueryFilters()
            .Select(s => new NegocioAdminListDto
            {
                SuscripcionId = s.Id,
                NegocioId = s.NegocioId,
                NombreNegocio = s.Negocio.Nombre,
                PlanNombre = s.PlanSuscripcion.Nombre,
                PlanSuscripcionId = s.PlanSuscripcionId,
                IsActive = s.Negocio.IsActive,
                FechaVencimiento = s.FechaVencimiento,
                MontoMensual = s.PrecioMensualPersonalizado ?? s.PlanSuscripcion.PrecioMensual
            })
            .OrderBy(s => s.NombreNegocio)
            .ToListAsync();

        return Ok(lista);
    }

    // 2. 🔥 NUEVO: Endpoint para traer los planes al Select del modal
    [HttpGet("planes")]
    public async Task<IActionResult> GetPlanes()
    {
        var planes = await _context.PlanesSuscripcion
            .Select(p => new PlanAdminDto { Id = p.Id, Nombre = p.Nombre })
            .ToListAsync();
        return Ok(planes);
    }

    // 3. Modificá el PUT para que ahora SÍ guarde el plan y la fecha
    [HttpPut("negocios/{negocioId}")]
    public async Task<IActionResult> UpdateNegocio(Guid negocioId, [FromBody] AdminUpdateNegocioDto dto)
    {
        var suscripcion = await _context.SuscripcionesNegocio
            .Include(s => s.Negocio)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

        if (suscripcion == null) return NotFound();

        suscripcion.Negocio.IsActive = dto.IsActive;
        suscripcion.PrecioMensualPersonalizado = dto.PrecioMensualPersonalizado;

        // 🔥 AHORA SÍ actualizamos plan y fecha:
        suscripcion.PlanSuscripcionId = dto.PlanSuscripcionId;
        suscripcion.Negocio.PlanSuscripcionId = dto.PlanSuscripcionId;
        suscripcion.FechaVencimiento = dto.FechaVencimiento.ToUniversalTime(); // Siempre a UTC para Postgres

        await _context.SaveChangesAsync();
        return Ok();
    }
}