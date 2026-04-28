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
        var lista = await (from n in _context.Negocios.IgnoreQueryFilters()
                           join s in _context.SuscripcionesNegocio.IgnoreQueryFilters()
                                on n.Id equals s.NegocioId into suscripciones
                           from s in suscripciones.DefaultIfEmpty()

                               // 🔥 BUSCAMOS AL DUEÑO (El usuario asociado a este negocio)
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
                               PlanSuscripcionId = n.PlanSuscripcionId,
                               PlanNombre = n.PlanSuscripcion != null ? n.PlanSuscripcion.Nombre : "Sin Plan",

                               MontoMensual = s != null && s.PrecioMensualPersonalizado.HasValue
                                   ? s.PrecioMensualPersonalizado.Value
                                   : (n.PlanSuscripcion != null ? n.PlanSuscripcion.PrecioMensual : 0),

                               // 🔥 DATOS DEL DUEÑO
                               OwnerName = owner != null ? owner.Nombre + " " + owner.Apellido : "Sin dueño",
                               OwnerEmail = owner != null ? owner.Email : "Sin email",
                               OwnerPhone = owner != null ? owner.PhoneNumber : ""
                           })
                           .ToListAsync(); // Quitamos el OrderBy de acá porque lo haremos en Blazor

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
        // 1. Buscamos el negocio (este siempre debería existir)
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        if (negocio == null) return NotFound("Negocio no encontrado.");

        // 2. Buscamos si ya tiene un registro de suscripción
        var suscripcion = await _context.SuscripcionesNegocio
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

        //  SI NO EXISTE, LA CREAMOS EN ESTE MOMENTO
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

        // 3. Actualizamos los datos (tanto del negocio como de la suscripción)
        negocio.IsActive = dto.IsActive;
        negocio.PlanSuscripcionId = dto.PlanSuscripcionId;

        suscripcion.PlanSuscripcionId = dto.PlanSuscripcionId;
        suscripcion.PrecioMensualPersonalizado = dto.PrecioMensualPersonalizado;
        suscripcion.FechaVencimiento = dto.FechaVencimiento.ToUniversalTime();
        suscripcion.Estado = EstadoSuscripcionEnum.Activa;

        await _context.SaveChangesAsync();
        return Ok();
    }
}