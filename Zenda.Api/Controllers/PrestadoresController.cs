using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Infrastructure;

[ApiController]
[Route("api/[controller]")]
public class PrestadoresController : ControllerBase
{
    private readonly ZendaDbContext _context;
    private readonly IMapper _mapper;

    public PrestadoresController(ZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    #region get

    [HttpGet("{slug}")]
    public async Task<ActionResult<PrestadorReadDto>> GetBySlug(string slug)
    {
        var prestador = await _context.Prestadores
            .Include(p => p.Horarios)
            .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug.ToLower());

        if (prestador == null) return NotFound(new { message = "Prestador no encontrado" });

        return Ok(_mapper.Map<PrestadorReadDto>(prestador));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrestadorReadDto>>> GetAll()
    {
        var prestadores = await _context.Prestadores.Include(p => p.Horarios).ToListAsync();
        // Mapeamos la lista de entidades a una lista de DTOs
        return Ok(_mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores));
    }
    #endregion

    #region Post
    [HttpPost]
    public async Task<ActionResult<PrestadorReadDto>> Create(PrestadorCreateDto dto)
    {
        // Convertimos el DTO en Entidad
        var prestador = _mapper.Map<Prestador>(dto);
        prestador.Id = Guid.NewGuid();

        _context.Prestadores.Add(prestador);
        await _context.SaveChangesAsync();

        // Devolvemos el ReadDto para no mostrar datos sensibles
        var resultado = _mapper.Map<PrestadorReadDto>(prestador);
        return CreatedAtAction(nameof(GetAll), new { id = resultado.Id }, resultado);
    }
    #endregion

    #region Put
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, PrestadorUpdateDto dto)
    {
        var prestadorDb = await _context.Prestadores.FindAsync(id);
        if (prestadorDb == null) return NotFound();

        // Mapeamos los cambios del DTO sobre la entidad que ya existe
        _mapper.Map(dto, prestadorDb);

        await _context.SaveChangesAsync();
        return NoContent(); // 204: Todo bien, pero no devuelvo contenido
    }
    #endregion

    #region delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var prestador = await _context.Prestadores.FindAsync(id);
        if (prestador == null) return NotFound();

        _context.Prestadores.Remove(prestador);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    #endregion
}