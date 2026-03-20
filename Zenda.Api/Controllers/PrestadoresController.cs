using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Api.DTOs;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PrestadorReadDto>>> GetAll()
    {
        var prestadores = await _context.Prestadores.ToListAsync();
        // Mapeamos la lista de entidades a una lista de DTOs
        return Ok(_mapper.Map<IEnumerable<PrestadorReadDto>>(prestadores));
    }

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
}