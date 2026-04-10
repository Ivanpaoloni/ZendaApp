using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services; // Asegurate de que el namespace coincida con el tuyo

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
        // 1. Validaciones de Negocio
        if (dto.HoraInicio >= dto.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");

        var prestador = await _context.Prestadores.AnyAsync(p => p.Id == dto.PrestadorId);
        if (!prestador) throw new ArgumentException("El prestador no existe.");

        // 2. Validación de Solapamiento (Overlap)
        bool haySolapamiento = await _context.Disponibilidad.AnyAsync(d =>
            d.PrestadorId == dto.PrestadorId &&
            d.DiaSemana == dto.DiaSemana &&
            dto.HoraInicio < d.HoraFin && d.HoraInicio < dto.HoraFin);

        if (haySolapamiento) throw new ArgumentException("El horario se superpone con uno existente.");

        // 3. Mapeo y Guardado
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
        // Verificamos existencia
        var existePrestador = await _context.Prestadores.AnyAsync(p => p.Id == prestadorId);
        if (!existePrestador) return false;

        // Limpieza de agenda anterior
        var actual = await _context.Disponibilidad
            .Where(d => d.PrestadorId == prestadorId)
            .ToListAsync();

        _context.Disponibilidad.RemoveRange(actual);

        // Mapeo masivo e inserción
        var nuevasDisponibilidades = agenda.Select(item => {
            var d = _mapper.Map<Disponibilidad>(item);
            d.Id = Guid.CreateVersion7(); // Reemplacé NewGuid por CreateVersion7 para seguir tu estándar
            d.PrestadorId = prestadorId;
            return d;
        }).ToList();

        _context.Disponibilidad.AddRange(nuevasDisponibilidades);

        return await _context.SaveChangesAsync() > 0;
    }

    // ==========================================
    // SECCIÓN DE BLOQUEOS (AUSENCIAS)
    // ==========================================

    public async Task<bool> CrearBloqueoAsync(BloqueoCreateDto dto)
    {
        // Validamos que el rango final (que ahora puede abarcar varios días) sea lógico
        if (dto.FinLocal <= dto.InicioLocal)
            throw new ArgumentException("La fecha/hora de fin debe ser posterior a la de inicio.");

        // 1. Buscamos la sede para saber su zona horaria y convertir a UTC
        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador == null || prestador.Sede == null)
            throw new ArgumentException("Prestador o Sede inválidos.");

        // Obtenemos la zona horaria real (Ej: "Argentina Standard Time" o "America/Argentina/Buenos_Aires")
        // Si por algún motivo está nula, usamos un fallback a UTC-3
        string tzId = !string.IsNullOrEmpty(prestador.Sede.ZonaHorariaId)
            ? prestador.Sede.ZonaHorariaId
            : "Argentina Standard Time";

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        // Convertimos las fechas elegidas (que pueden abarcar 15 días) a UTC absoluto
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
        if (prestador == null) return new List<BloqueoReadDto>();

        string tzId = !string.IsNullOrEmpty(prestador.Sede?.ZonaHorariaId) ? prestador.Sede.ZonaHorariaId : "Argentina Standard Time";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        // Traemos todos los bloqueos que todavía no terminaron
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
            // Convertimos la hora UTC de vuelta a la hora local para que la UI diga "Lunes 14:00"
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

        // 1. Consulta Rápida: Traemos los bloqueos activos de todos los prestadores del negocio
        // Filtramos por FinUtc > ahoraUtc para no traer bloqueos viejos
        var bloqueosActivosDb = await _context.BloqueosAgenda
            .Include(b => b.Prestador)
            .ThenInclude(p => p.Sede)
            .Where(b => b.Prestador.NegocioId == negocioId && b.FinUtc > ahoraUtc)
            .ToListAsync();

        var ausenciasHoy = new List<BloqueoReadDto>();

        // 2. Filtro Preciso en Memoria (Zona Horaria)
        // Por qué en memoria? Porque necesitamos la zona horaria específica de CADA sede 
        // para saber si "Hoy localmente" se cruza con este bloqueo de varios días.
        foreach (var b in bloqueosActivosDb)
        {
            string tzId = !string.IsNullOrEmpty(b.Prestador.Sede?.ZonaHorariaId)
                ? b.Prestador.Sede.ZonaHorariaId
                : "Argentina Standard Time";

            var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, zonaSede);
            var inicioLocal = TimeZoneInfo.ConvertTimeFromUtc(b.InicioUtc, zonaSede);
            var finLocal = TimeZoneInfo.ConvertTimeFromUtc(b.FinUtc, zonaSede);

            // Armamos el inicio del "Día de Hoy" a las 00:00 y fin a las 23:59:59 local
            var inicioDiaLocal = ahoraLocal.Date;
            var finDiaLocal = ahoraLocal.Date.AddDays(1);

            // Fórmula maestra de superposición: (Empieza antes de que termine hoy) Y (Termina después de la hora actual)
            if (inicioLocal < finDiaLocal && finLocal > ahoraLocal)
            {
                ausenciasHoy.Add(new BloqueoReadDto
                {
                    Id = b.Id,
                    PrestadorId = b.PrestadorId,
                    // Devolvemos el nombre para la UI del dashboard
                    Motivo = $"{b.Prestador.Nombre}: {b.Motivo}",
                    InicioLocal = inicioLocal,
                    FinLocal = finLocal
                });
            }
        }

        return ausenciasHoy.OrderBy(a => a.InicioLocal);
    }
}