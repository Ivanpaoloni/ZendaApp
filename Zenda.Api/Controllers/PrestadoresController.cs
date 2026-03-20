using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.Entities;
using Zenda.Infrastructure;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrestadoresController : ControllerBase
{
    private readonly ZendaDbContext _context;

    // Inyectamos el contexto de la base de datos
    public PrestadoresController(ZendaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Traemos los prestadores de la base de datos real
        var prestadores = await _context.Prestadores.Include(x => x.Horarios).ToListAsync();
        return Ok(prestadores);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Prestador nuevo)
    {
        nuevo.Id = Guid.NewGuid();

        // Lo guardamos en Neon
        _context.Prestadores.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, nuevo);
    }
}