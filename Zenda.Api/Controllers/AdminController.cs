using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs.Admin;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

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

    [HttpGet("negocios")]
    public async Task<IActionResult> GetNegociosAdmin()
    {
        // Utilizamos LINQ to Entities para armar la proyección. 
        // EF Core traducirá esto a sentencias LEFT JOIN optimizadas en SQL.
        var lista = await (from n in _context.Negocios.IgnoreQueryFilters()

                               // 🔥 Opcional pero recomendado: Filtrar solo la suscripción "Activa" 
                               // para evitar duplicados en la grilla si el negocio tiene historial.
                           join s in _context.SuscripcionesNegocio.IgnoreQueryFilters()
                                .Where(sub => sub.Estado == EstadoSuscripcionEnum.Activa)
                                on n.Id equals s.NegocioId into suscripciones
                           from s in suscripciones.DefaultIfEmpty()

                               // Buscamos al dueño
                           let owner = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.NegocioId == n.Id)

                           select new NegocioAdminListDto
                           {
                               NegocioId = n.Id,
                               NombreNegocio = n.Nombre,
                               IsActive = n.IsActive,
                               Slug = n.Slug,
                               CreatedAtUtc = n.CreatedAtUtc,

                               SuscripcionId = s != null ? s.Id : Guid.Empty,
                               FechaVencimiento = s != null ? s.FechaVencimiento : DateTime.MinValue,

                               // 🔥 EL FIX: Ahora sacamos los datos del plan desde la Suscripción (s) y no del Negocio (n)
                               PlanSuscripcionId = s != null ? s.PlanSuscripcionId : Guid.Empty,
                               PlanNombre = s != null && s.PlanSuscripcion != null ? s.PlanSuscripcion.Nombre : "Sin Plan",

                               // 🔥 EL FIX: Calculamos el monto también desde la Suscripción
                               MontoMensual = s != null && s.PrecioMensualPersonalizado.HasValue
                                   ? s.PrecioMensualPersonalizado.Value
                                   : (s != null && s.PlanSuscripcion != null ? s.PlanSuscripcion.PrecioMensual : 0),

                               // Datos del dueño
                               OwnerName = owner != null ? owner.Nombre + " " + owner.Apellido : "Sin dueño",
                               OwnerEmail = owner != null ? owner.Email : "Sin email",
                               OwnerPhone = owner != null ? owner.PhoneNumber : ""
                           })
                           .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("planes")]
    public async Task<IActionResult> GetPlanes()
    {
        var planes = await _context.PlanesSuscripcion
            .Select(p => new PlanAdminDto { Id = p.Id, Nombre = p.Nombre })
            .ToListAsync();
        return Ok(planes);
    }

    [HttpPut("negocios/{negocioId}")]
    public async Task<IActionResult> UpdateNegocio(Guid negocioId, [FromBody] AdminUpdateNegocioDto dto)
    {
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        if (negocio == null) return NotFound("Negocio no encontrado.");

        var suscripcion = await _context.SuscripcionesNegocio
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId && s.Estado == EstadoSuscripcionEnum.Activa);

        if (suscripcion == null)
        {
            suscripcion = new SuscripcionNegocio
            {
                Id = Guid.NewGuid(),
                NegocioId = negocioId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.SuscripcionesNegocio.Add(suscripcion);
        }

        // 1. Actualizamos el Negocio
        negocio.IsActive = dto.IsActive;
        // 🔥 EL FIX: Eliminamos la línea "negocio.PlanSuscripcionId = dto.PlanSuscripcionId;"

        // 2. Actualizamos la Suscripción
        suscripcion.PlanSuscripcionId = dto.PlanSuscripcionId;
        suscripcion.PrecioMensualPersonalizado = dto.PrecioMensualPersonalizado;
        suscripcion.FechaVencimiento = dto.FechaVencimiento.ToUniversalTime();
        suscripcion.Estado = EstadoSuscripcionEnum.Activa;

        await _context.SaveChangesAsync();
        return Ok();
    }
}