using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zenda.Api.DTOs;
using Zenda.Core.Entities;
using Zenda.Infrastructure;

namespace Zenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurnosController : ControllerBase
{
    private readonly ZendaDbContext _context;
    private readonly IMapper _mapper;

    public TurnosController(ZendaDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    #region Post (Reserva de Turno)
    [HttpPost]
    public async Task<ActionResult<TurnoReadDto>> Create(TurnoCreateDto dto)
    {
        // BUENA PRÁCTICA: Normalizar a "Minuto Cero" o "Múltiplos de 15/30"
        // Limpiamos segundos y milisegundos para que las comparaciones en la DB sean exactas
        var inicioLimpio = new DateTime(
            dto.Inicio.Year, dto.Inicio.Month, dto.Inicio.Day,
            dto.Inicio.Hour, dto.Inicio.Minute, 0, DateTimeKind.Utc);

        var finSolicitado = inicioLimpio.AddMinutes(30);
        var diaSemana = (int)inicioLimpio.DayOfWeek;
        var horaInicio = TimeOnly.FromDateTime(inicioLimpio);
        var horaFin = TimeOnly.FromDateTime(finSolicitado);

        // 1. Obtenemos el Enum puro
        var diaSemanaTurno = dto.Inicio.DayOfWeek;

        // 2. Comparamos directamente
        var atiende = await _context.Disponibilidad
            .AnyAsync(d => d.PrestadorId == dto.PrestadorId &&
                           d.DiaSemana == diaSemanaTurno &&
                           horaInicio >= d.HoraInicio &&
                           horaFin <= d.HoraFin);

        if (!atiende)
            return BadRequest("El profesional no tiene disponibilidad configurada para ese horario.");

        // 3. VALIDACIÓN 2: ¿El turno se solapa con otro ya existente?
        // Lógica de solapamient6o: (InicioA < FinB) Y (InicioB < FinA)
        var ocupado = await _context.Turnos
            .AnyAsync(t => t.PrestadorId == dto.PrestadorId &&
                           inicioLimpio < t.Fin &&
                           t.Inicio < finSolicitado);

        if (ocupado)
            return BadRequest("El horario ya se encuentra ocupado por otro turno.");

        // 4. Mapeo y Guardado
        var turno = _mapper.Map<Turno>(dto); // Acá AutoMapper ya mapea PrestadorId, Inicio, etc.
        turno.Id = Guid.NewGuid();
        turno.Fin = finSolicitado; // Asegurate que esto se guarde bien
        turno.EstaConfirmado = false;

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<TurnoReadDto>(turno));
    }
    #endregion

    #region Get (Listado por Prestador)
    [HttpGet("prestador/{prestadorId}")]
    public async Task<ActionResult<IEnumerable<TurnoReadDto>>> GetByPrestador(Guid prestadorId)
    {
        var turnos = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId)
            .OrderBy(t => t.Inicio)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<TurnoReadDto>>(turnos));
    }
    #endregion
}