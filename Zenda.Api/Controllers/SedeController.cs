using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SedesController : ControllerBase
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;

    public SedesController(IZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SedeReadDto>>> GetAll()
    {
        // Traemos todas las sedes que no estén borradas (Soft Delete)
        var sedes = await _context.Sedes
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<SedeReadDto>>(sedes));
    }
}