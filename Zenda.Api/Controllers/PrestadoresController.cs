using Microsoft.AspNetCore.Mvc;
using Zenda.Core.Entities; // Asegurate de tener las entidades que definimos antes

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrestadoresController : ControllerBase
{
    // Por ahora usamos una lista en memoria hasta tener la DB
    private static readonly List<Prestador> _prestadores = new()
    {
        new Prestador { Id = Guid.NewGuid(), Nombre = "Ivan Paoloni", Especialidad = "Software Dev", Slug = "ivan-dev" }
    };

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_prestadores);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Prestador nuevo)
    {
        nuevo.Id = Guid.NewGuid();
        _prestadores.Add(nuevo);
        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, nuevo);
    }
}