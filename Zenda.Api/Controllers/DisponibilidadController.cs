using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Infrastructure;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisponibilidadController : ControllerBase
{
    private readonly ZendaDbContext _context;
    private readonly IMapper _mapper;

    public DisponibilidadController(ZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    #region Get
    [HttpGet("prestador/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<DisponibilidadReadDto>>> GetByPrestador(Guid prestadorId)
    {
        var horarios = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<DisponibilidadReadDto>>(horarios));
    }
    #endregion

    #region Post
    [HttpPost]
    public async Task<ActionResult<DisponibilidadReadDto>> Create(DisponibilidadCreateDto dto)
    {
        // TODO: Validar que el prestador exista y que no se superpongan horarios
        var disponibilidad = _mapper.Map<Disponibilidad>(dto);
        disponibilidad.Id = Guid.NewGuid();

        _context.Disponibilidad.Add(disponibilidad);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<DisponibilidadReadDto>(disponibilidad));
    }
    #endregion

    #region Delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var disp = await _context.Disponibilidad.FindAsync(id);
        if (disp == null) return NotFound();

        _context.Disponibilidad.Remove(disp);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion
}