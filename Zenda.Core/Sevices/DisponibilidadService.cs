using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class DisponibilidadService : IDisponibilidadService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantService _tenantService;

    public DisponibilidadService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    public async Task<IEnumerable<DisponibilidadReadDto>> GetByPrestadorAsync(Guid prestadorId)
    {
        var horarios = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .ToListAsync();

        return _mapper.Map<IEnumerable<DisponibilidadReadDto>>(horarios);
    }

    public async Task<DisponibilidadReadDto> CreateAsync(DisponibilidadCreateDto dto)
    {
        if (dto.HoraInicio >= dto.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");

        var prestador = await _context.Prestadores.AnyAsync(p => p.Id == dto.PrestadorId);

        if (!prestador)
            throw new ArgumentException("El prestador no existe.");

        bool haySolapamiento = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == dto.DiaSemana &&
            dto.HoraInicio < d.HoraFin && d.HoraInicio < dto.HoraFin);

        if (haySolapamiento)
            throw new ArgumentException("El horario se superpone con uno existente.");

        var disponibilidad = _mapper.Map<Disponibilidad>(dto);
        disponibilidad.Id = Guid.CreateVersion7();

        _context.Disponibilidad.Add(disponibilidad);
        await _context.SaveChangesAsync();

        return _mapper.Map<DisponibilidadReadDto>(disponibilidad);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var disp = await _context.Disponibilidad.FirstOrDefaultAsync(x => x.Id == id);
        if (disp == null) return false;

        _context.Disponibilidad.Remove(disp);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpsertAgendaAsync(Guid prestadorId, IEnumerable<DisponibilidadCreateDto> agenda)
    {
        var existePrestador = await _context.Prestadores.AnyAsync(p => p.Id == prestadorId);

        if (!existePrestador)
            return false;

        var actual = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .ToListAsync();

        _context.Disponibilidad.RemoveRange(actual);

        var nuevasDisponibilidades = agenda.Select(item =>
        {
            var d = _mapper.Map<Disponibilidad>(item);
            d.Id = Guid.CreateVersion7();
            d.PrestadorId = prestadorId;
            return d;
        }).ToList();

        _context.Disponibilidad.AddRange(nuevasDisponibilidades);

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto)
    {
        if (dto.FinLocal <= dto.InicioLocal)
            throw new ArgumentException("La fecha/hora de fin debe ser posterior a la de inicio.");

        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador == null || prestador.Sede == null)
            throw new ArgumentException("Prestador o Sede inválidos.");

        string tzId = !string.IsNullOrEmpty(prestador.Sede.ZonaHorariaId) ? prestador.Sede.ZonaHorariaId : "Argentina Standard Time";

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var inicioCrudo = DateTime.SpecifyKind(dto.InicioLocal, DateTimeKind.Unspecified);
        var finCrudo = DateTime.SpecifyKind(dto.FinLocal, DateTimeKind.Unspecified);

        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioCrudo, zonaSede);
        var finUtc = TimeZoneInfo.ConvertTimeToUtc(finCrudo, zonaSede);

        var bloqueo = new BloqueoAgenda
        {
            Id = Guid.CreateVersion7(),
            PrestadorId = dto.PrestadorId,
            SedeId = dto.SedeId,
            InicioUtc = inicioUtc,
            FinUtc = finUtc,
            Motivo = dto.Motivo
        };

        _context.BloqueosAgenda.Add(bloqueo);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<BloqueoReadDto>> GetBloqueosFuturosAsync(Guid prestadorId)
    {
        var prestador = await _context.Prestadores.Include(p => p.Sede).FirstOrDefaultAsync(p => p.Id == prestadorId);

        if (prestador == null)
            return new List<BloqueoReadDto>();

        string tzId = !string.IsNullOrEmpty(prestador.Sede?.ZonaHorariaId) ? prestador.Sede.ZonaHorariaId : "Argentina Standard Time";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var bloqueos = await _context.BloqueosAgenda
            .Where(b => b.PrestadorId == prestadorId && b.FinUtc >= DateTime.UtcNow)
            .OrderBy(b => b.InicioUtc)
            .ToListAsync();

        return bloqueos.Select(b => new BloqueoReadDto
        {
            Id = b.Id,
            PrestadorId = b.PrestadorId,
            SedeId = b.SedeId,
            Motivo = b.Motivo,
            InicioLocal = TimeZoneInfo.ConvertTimeFromUtc(b.InicioUtc, zonaSede),
            FinLocal = TimeZoneInfo.ConvertTimeFromUtc(b.FinUtc, zonaSede)
        });
    }

    public async Task<bool> EliminarBloqueoAsync(Guid id)
    {
        var bloqueo = await _context.BloqueosAgenda.FindAsync(id);
        if (bloqueo == null) return false;

        _context.BloqueosAgenda.Remove(bloqueo);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<BloqueoReadDto>> GetBloqueosDeHoyAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        var ahoraUtc = DateTime.UtcNow;

        var bloqueosActivosDb = await _context.BloqueosAgenda
            .Include(b => b.Prestador)
            .ThenInclude(p => p.Sede)
            .Where(b => b.Prestador.NegocioId == negocioId && b.FinUtc > ahoraUtc)
            .ToListAsync();

        var ausenciasHoy = new List<BloqueoReadDto>();

        foreach (var b in bloqueosActivosDb)
        {
            string tzId = !string.IsNullOrEmpty(b.Prestador.Sede?.ZonaHorariaId)
                ? b.Prestador.Sede.ZonaHorariaId
                : "Argentina Standard Time";

            var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, zonaSede);
            var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(b.InicioUtc, zonaSede);
            var finLocal = TimeZoneInfo.ConvertTimeFromUtc(b.FinUtc, zonaSede);

            var inicioDiaLocal = ahoraLocal.Date;
            var finDiaLocal = ahoraLocal.Date.AddDays(1);

            if (inicioLocal < finDiaLocal && finLocal > ahoraLocal)
            {
                ausenciasHoy.Add(new BloqueoReadDto
                {
                    Id = b.Id,
                    PrestadorId = b.PrestadorId,
                    Motivo = $"{b.Prestador.Nombre}: {b.Motivo}",
                    InicioLocal = inicioLocal,
                    FinLocal = finLocal
                });
            }
        }

        return ausenciasHoy.OrderBy(a => a.InicioLocal);
    }
}