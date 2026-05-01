using AutoMapper;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

namespace Zenda.Application.Services;

public class TurnosService : ITurnosService
{
    private readonly IZendaDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITenantService _tenantService;
    private readonly IMapper _mapper;
    private readonly IJobService _jobService;

    public TurnosService(IZendaDbContext context, IMapper mapper, ITenantService tenantService, IEmailService emailService, IJobService jobService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
        _emailService = emailService;
        _jobService = jobService;
    }

    public async Task<TurnoReadDto> GetByIdAsync(Guid id)
    {
        var turno = await _context.Turnos.FindAsync(id);

        if (turno == null)
            return new();

        return _mapper.Map<TurnoReadDto>(turno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetByPrestadorAsync(Guid prestadorId)
    {
        var turnos = await _context.Turnos
            .Where(t => t.PrestadorId == prestadorId)
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TurnoReadDto>>(turnos);
    }

    public async Task<DisponibilidadFechaDto> GetDisponibilidadAsync(Guid? prestadorId, Guid sedeId, DateTime fecha, Guid servicioId)
    {
        var respuesta = new DisponibilidadFechaDto { Fecha = fecha.Date };

        // 1. Buscamos prestadores de esta sede que brinden el servicio seleccionado
        var queryPrestadores = _context.Prestadores
            .IgnoreQueryFilters()
            .Include(p => p.Sede)
            .Include(p => p.Negocio)
            .Where(p => p.SedeId == sedeId && p.Servicios.Any(s => s.Id == servicioId));

        // Si pasaron un ID específico, filtramos solo a ese
        if (prestadorId.HasValue)
            queryPrestadores = queryPrestadores.Where(p => p.Id == prestadorId.Value);

        var prestadores = await queryPrestadores.ToListAsync();
        if (!prestadores.Any()) return respuesta;

        var primerPrestador = prestadores.First();
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(primerPrestador.Sede.ZonaHorariaId);
        int anticipacionHoras = primerPrestador.Negocio.AnticipacionMinimaHoras;

        // Escudos de fechas
        var fechaHoraActualSede = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);
        if (fecha.Date < fechaHoraActualSede.Date) return respuesta;

        var barreraAnticipacion = fechaHoraActualSede.AddHours(anticipacionHoras);
        if (fecha.Date < barreraAnticipacion.Date) return respuesta;

        var horaMinimaPermitida = TimeOnly.FromDateTime(barreraAnticipacion);
        bool aplicaBarreraHoy = fecha.Date == barreraAnticipacion.Date;

        // Duración del servicio
        int duracionServicio = 30; // Fallback
        var servicio = await _context.Servicios.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == servicioId);
        if (servicio != null && servicio.DuracionMinutos > 0) duracionServicio = servicio.DuracionMinutos;

        var inicioDiaLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        var finDiaLocal = inicioDiaLocal.AddDays(1);
        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaSede);
        var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaSede);
        int diaBuscado = (int)fecha.DayOfWeek;

        // Diccionario temporal para guardar Hora -> Prestador (Si 2 prestadores tienen la 10:30, gana el primero que la ofrezca)
        var turnosConsolidados = new Dictionary<string, Guid>();

        foreach (var prestador in prestadores)
        {
            int intervaloGrillaMinutos = prestador.Negocio.IntervaloTurnosMinutos > 0 ? prestador.Negocio.IntervaloTurnosMinutos : 30;

            var configuracion = await _context.Disponibilidad
                .IgnoreQueryFilters().Where(d => d.PrestadorId == prestador.Id && d.DiaSemana == diaBuscado).OrderBy(d => d.HoraInicio).ToListAsync();

            var turnosOcupados = await _context.Turnos
                .IgnoreQueryFilters()
                .Where(t => t.PrestadorId == prestador.Id && t.FechaHoraInicioUtc >= inicioDiaUtc && t.FechaHoraInicioUtc < finDiaUtc && t.Estado != EstadoTurnoEnum.Cancelado)
                .Select(t => new { t.FechaHoraInicioUtc, t.FechaHoraFinUtc }).ToListAsync();

            var bloqueos = await _context.BloqueosAgenda
                .IgnoreQueryFilters()
                .Where(b => b.PrestadorId == prestador.Id && b.InicioUtc < finDiaUtc && b.FinUtc > inicioDiaUtc).ToListAsync();

            foreach (var rango in configuracion)
            {
                var inicioSlot = rango.HoraInicio;
                while (inicioSlot.AddMinutes(duracionServicio) <= rango.HoraFin)
                {
                    var finSlot = inicioSlot.AddMinutes(duracionServicio);
                    var slotInicioLocal = inicioDiaLocal.Add(inicioSlot.ToTimeSpan());
                    var slotFinLocal = inicioDiaLocal.Add(finSlot.ToTimeSpan());
                    var slotInicioUtc = TimeZoneInfo.ConvertTimeToUtc(slotInicioLocal, zonaSede);
                    var slotFinUtc = TimeZoneInfo.ConvertTimeToUtc(slotFinLocal, zonaSede);

                    bool muyPronto = aplicaBarreraHoy && inicioSlot <= horaMinimaPermitida;
                    bool estaOcupado = turnosOcupados.Any(t => t.FechaHoraInicioUtc < slotFinUtc && slotInicioUtc < t.FechaHoraFinUtc);
                    bool estaBloqueado = bloqueos.Any(b => b.InicioUtc < slotFinUtc && slotInicioUtc < b.FinUtc);

                    if (!estaOcupado && !estaBloqueado && !muyPronto)
                    {
                        // Lo agregamos si nadie más cubrió esta hora aún
                        turnosConsolidados.TryAdd(inicioSlot.ToString("HH:mm"), prestador.Id);
                    }
                    inicioSlot = inicioSlot.AddMinutes(intervaloGrillaMinutos);
                }
            }
        }

        respuesta.HorariosLibres = turnosConsolidados
            .Select(tc => new HorarioDisponibleDto { Hora = tc.Key, PrestadorId = tc.Value })
            .OrderBy(x => TimeOnly.Parse(x.Hora))
            .ToList();

        return respuesta;
    }
    public async Task<TurnoReadDto> ReservarTurnoAsync(TurnoCreateDto dto)
    {
        var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == dto.ServicioId);

        if (servicio == null)
            throw new Exception("El servicio seleccionado no existe o no está disponible.");

        var prestador = await _context.Prestadores
            .IgnoreQueryFilters()
            .Include(p => p.Sede)
            .Include(p => p.Horarios)
            .Include(p => p.Negocio)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId);

        if (prestador?.Sede == null || prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador, Sede o Negocio no encontrados.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        var fechaCruda = DateTime.SpecifyKind(dto.Inicio, DateTimeKind.Unspecified);
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaCruda, zonaSede);
        var fechaFinUtcDefinitiva = fechaUtcDefinitiva.AddMinutes(servicio.DuracionMinutos);

        // ==========================================
        // BARRERAS DE VALIDACIÓN DEL NEGOCIO (BACKEND)
        // ==========================================

        int anticipacion = prestador.Negocio.AnticipacionMinimaHoras;

        // BARRERA 1: Anticipación
        if (fechaUtcDefinitiva < DateTime.UtcNow.AddHours(anticipacion))
        {
            throw new InvalidOperationException($"Debe reservar con al menos {anticipacion} horas de anticipación.");
        }

        // BARRERA 2: Horario de trabajo
        int diaSemana = (int)fechaCruda.DayOfWeek;
        var horaSolicitada = TimeOnly.FromDateTime(fechaCruda);
        var horaFinSolicitada = horaSolicitada.AddMinutes(servicio.DuracionMinutos);

        // Validamos que el turno solicitado entre en ALGUNO de los rangos habilitados para ese día
        bool turnoDentroDeHorario = prestador.Horarios
            .Where(h => h.DiaSemana == diaSemana)
            .Any(h => horaSolicitada >= h.HoraInicio && horaFinSolicitada <= h.HoraFin);

        if (!turnoDentroDeHorario)
        {
            throw new InvalidOperationException("El horario solicitado está fuera de la jornada laboral o cae en un horario de descanso.");
        }

        // BARRERA 3: Choque de Turnos
        bool turnoOcupado = await _context.Turnos.IgnoreQueryFilters().AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            t.Estado != EstadoTurnoEnum.Cancelado &&
            (fechaUtcDefinitiva < t.FechaHoraFinUtc && fechaFinUtcDefinitiva > t.FechaHoraInicioUtc)
        );

        if (turnoOcupado)
        {
            throw new InvalidOperationException("Lo sentimos, este horario acaba de ser reservado.");
        }

        // 🎯 NUEVA BARRERA 4: Choque de Vacaciones/Bloqueos (Seguridad extra)
        bool chocaConBloqueo = await _context.BloqueosAgenda.IgnoreQueryFilters().AnyAsync(b =>
            b.PrestadorId == dto.PrestadorId &&
            b.InicioUtc < fechaFinUtcDefinitiva &&
            fechaUtcDefinitiva < b.FinUtc
        );

        if (chocaConBloqueo)
        {
            throw new InvalidOperationException("El horario solicitado se encuentra bloqueado por vacaciones o ausencia del profesional.");
        }

        // ==========================================
        // SI PASA TODAS LAS BARRERAS, GUARDAMOS
        // ==========================================
        var emailNormalizado = dto.EmailClienteInvitado.Trim().ToLower();

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.NegocioId == prestador.NegocioId && c.Email.ToLower() == emailNormalizado);

        if (cliente == null)
        {
            cliente = new Cliente
            {
                Id = Guid.CreateVersion7(),
                NegocioId = prestador.NegocioId,
                Nombre = dto.NombreClienteInvitado.Trim(),
                Email = emailNormalizado,
                Telefono = dto.TelefonoClienteInvitado?.Trim()
            };
            _context.Clientes.Add(cliente);
        }
        else if (!string.IsNullOrWhiteSpace(dto.TelefonoClienteInvitado) && cliente.Telefono != dto.TelefonoClienteInvitado)
        {
            cliente.Telefono = dto.TelefonoClienteInvitado.Trim();
        }

        // 🎯 MEJORA 1: Generamos el ID del turno en memoria ANTES de guardar.
        // Al usar Guid.CreateVersion7(), no necesitamos ir a la BD para obtener el ID.
        var turnoId = Guid.CreateVersion7();
        var fechaRecordatorioUtc = fechaUtcDefinitiva.AddHours(-1);
        string? recordatorioJobId = null;

        // 🎯 MEJORA 2: Programamos el Job de Hangfire en memoria.
        // Si falla, el turno aún no se ha guardado.
        if (fechaRecordatorioUtc > DateTime.UtcNow)
        {
            recordatorioJobId = _jobService.ProgramarRecordatorioEmail(
                dto.EmailClienteInvitado,
                dto.NombreClienteInvitado,
                prestador.Negocio.Nombre,
                dto.Inicio,
                fechaRecordatorioUtc,
                turnoId // Usamos el ID generado arriba
            );
        }

        var nuevoTurno = new Turno
        {
            Id = turnoId,
            NegocioId = prestador.NegocioId,
            PrestadorId = dto.PrestadorId,
            FechaHoraInicioUtc = fechaUtcDefinitiva,
            FechaHoraFinUtc = fechaFinUtcDefinitiva,
            ClienteId = cliente.Id,
            Estado = EstadoTurnoEnum.Confirmado,
            ServicioId = dto.ServicioId,
            RecordatorioJobId = recordatorioJobId // Lo asignamos de una sola vez
        };

        _context.Turnos.Add(nuevoTurno);

        // 🎯 MEJORA 3: Un solo SaveChangesAsync. (Unit of Work real)
        await _context.SaveChangesAsync();

        // 🎯 MEJORA 4: Resiliencia (Fault Tolerance) con el Email
        // El correo es un proceso secundario. Si falla, la reserva ya está confirmada.
        try
        {
            await _emailService.EnviarConfirmacionTurnoAsync(
                dto.EmailClienteInvitado,
                dto.NombreClienteInvitado,
                prestador.Negocio.Nombre,
                dto.Inicio,
                nuevoTurno.Id,
                servicio.Nombre,
                prestador.Nombre,
                prestador.Sede.Nombre,
                prestador.Sede.Direccion
            );
        }
        catch (Exception ex)
        {
            // Aquí deberías registrar el error con un ILogger<TurnosService>
            // _logger.LogError(ex, "El turno se guardó, pero falló el envío del email de confirmación para {Email}", dto.EmailClienteInvitado);

            // IMPORTANTE: No lanzamos el throw. El usuario ya tiene su turno asegurado.
        }

        return _mapper.Map<TurnoReadDto>(nuevoTurno);
    }

    public async Task<IEnumerable<TurnoReadDto>> GetTurnosByFechaAsync(DateTime fecha)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        var inicioDiaLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        var finDiaLocal = inicioDiaLocal.AddDays(1);

        var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(inicioDiaLocal, zonaSede);
        var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(finDiaLocal, zonaSede);

        var turnos = await _context.Turnos
        .AsNoTracking()
        .Include(t => t.Servicio)
        .Include(t => t.Prestador!.Sede)
        .Where(t => t.FechaHoraInicioUtc >= inicioDiaUtc && t.FechaHoraInicioUtc < finDiaUtc)
        .OrderBy(t => t.FechaHoraInicioUtc)
        .Select(t => new TurnoReadDto
        {
            Id = t.Id,

            ClienteId = t.ClienteId,
            ClienteNombre = t.Cliente.Nombre,
            ClienteEmail = t.Cliente.Email,
            ClienteTelefono = t.Cliente.Telefono,

            PrestadorId = t.PrestadorId,
            PrestadorNombre = t.Prestador!.Nombre,

            ServicioId = t.ServicioId,
            ServicioNombre = t.Servicio.Nombre,
            Precio = t.Servicio.Precio,
            DuracionMinutos = t.Servicio.DuracionMinutos,

            FechaHoraInicioUtc = t.FechaHoraInicioUtc,
            FechaHoraFinUtc = t.FechaHoraFinUtc,
            Estado = t.Estado,

            SedeNombre = t.Prestador!.Sede!.Nombre,
            NegocioSlug = t.Prestador!.Negocio!.Slug
        })
        .ToListAsync();

        return turnos;
    }

    public async Task<bool> CambiarEstadoAsync(Guid turnoId, EstadoTurnoEnum nuevoEstado)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var turno = await _context.Turnos
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Include(t => t.Cliente)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.Prestador.NegocioId == negocioId);

        if (turno == null) return false;

        if (turno.Estado == EstadoTurnoEnum.Completado && nuevoEstado == EstadoTurnoEnum.Pendiente)
        {
            throw new Exception("No se puede reabrir un turno ya finalizado.");
        }

        // 🎯 1. APLICAMOS EL CAMBIO DE ESTADO PRIMERO
        turno.Estado = nuevoEstado;

        // 🎯 2. GUARDAMOS EN BASE DE DATOS PARA GARANTIZAR LA REGLA DE NEGOCIO
        var exito = await _context.SaveChangesAsync() > 0;

        // 🎯 3. PROCESAMOS LOS EFECTOS SECUNDARIOS SOLO SI SE GUARDÓ CON ÉXITO
        if (exito && nuevoEstado == EstadoTurnoEnum.Cancelado)
        {
            // Matamos el recordatorio de Hangfire
            if (!string.IsNullOrEmpty(turno.RecordatorioJobId))
            {
                try
                {
                    _jobService.CancelarTrabajo(turno.RecordatorioJobId);
                    // Si quisieras limpiar el ID en la BD, requeriría otro SaveChangesAsync, 
                    // pero al estar cancelado el turno, no es estrictamente necesario.
                }
                catch (Exception ex)
                {
                    // Loguear error de Hangfire
                }
            }

            // Enviamos el correo protegiendo el flujo principal
            if (!string.IsNullOrEmpty(turno.Cliente.Email) && turno.Prestador?.Sede != null && turno.Prestador?.Negocio != null)
            {
                try
                {
                    var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(turno.Prestador.Sede.ZonaHorariaId);
                    var inicioSedeLocal = TimeZoneInfo.ConvertTimeFromUtc(turno.FechaHoraInicioUtc, zonaSede);

                    await _emailService.EnviarCancelacionTurnoAsync(
                        turno.Cliente.Email,
                        turno.Cliente.Nombre,
                        turno.Prestador.Negocio.Nombre,
                        inicioSedeLocal,
                        turno.Prestador.Negocio.Slug
                    );
                }
                catch (Exception ex)
                {
                    // ⚠️ AQUÍ ESTÁ LA MAGIA:
                    // Si el email falla (dirección inválida, caída del servicio), capturamos el error.
                    // El turno YA está cancelado en la base de datos, que es lo que realmente importa.
                    // TODO: _logger.LogWarning(ex, "Falló el envío de correo de cancelación al cliente.");
                }
            }
        }

        return exito;
    }

    // Para la vista pública de gestión
    public async Task<TurnoReadDto> GetResumenPublicoAsync(Guid turnoId)
    {
        var turno = await _context.Turnos
            .IgnoreQueryFilters()
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
                .Include(t => t.Cliente)
            // NUEVO: Traemos el negocio para sacar el Slug
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .Include(t => t.Servicio)
            .FirstOrDefaultAsync(t => t.Id == turnoId);

        if (turno == null) return null;

        return new TurnoReadDto
        {
            Id = turno.Id,
            FechaHoraInicioUtc = turno.FechaHoraInicioUtc,
            Estado = turno.Estado,
            ClienteNombre = turno.Cliente.Nombre,
            ClienteTelefono = turno.Cliente.Telefono,
            ClienteEmail = turno.Cliente.Email,
            PrestadorNombre = turno.Prestador.Nombre,
            ServicioNombre = turno.Servicio.Nombre,
            SedeNombre = turno.Prestador.Sede?.Nombre ?? "",
            NegocioSlug = turno.Prestador.Negocio?.Slug ?? ""
        };
    }

    // Para la acción de cancelar desde el mail
    public async Task<bool> CancelarPorClienteAsync(Guid turnoId)
    {
        // Juntamos la búsqueda del turno y los datos del negocio en una sola query
        var turno = await _context.Turnos
            .IgnoreQueryFilters()
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Negocio)
            .FirstOrDefaultAsync(t => t.Id == turnoId);

        if (turno == null) return false;

        if (turno.Prestador?.Sede == null || turno.Prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador, Sede o Negocio no encontrados.");

        // Validaciones de tiempo
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(turno.Prestador.Sede.ZonaHorariaId);
        var ahoraSede = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);
        var inicioSedeLocal = TimeZoneInfo.ConvertTimeFromUtc(turno.FechaHoraInicioUtc, zonaSede);

        // TODO: PARAMETRIZAR
        int horasMinimas = 2;
        if (inicioSedeLocal <= ahoraSede.AddHours(horasMinimas))
        {
            throw new Exception($"No es posible cancelar el turno, la anticipación minima es de {horasMinimas} horas. Por favor comuníquese por teléfono.");
        }

        if (turno.Estado == EstadoTurnoEnum.Completado)
        {
            throw new Exception("El turno ya figura como completado.");
        }

        // Si pasa todo, cancelamos
        turno.Estado = EstadoTurnoEnum.Cancelado;
        var exito = await _context.SaveChangesAsync() > 0;

        // 🎯 SI SE GUARDÓ LA CANCELACIÓN, ENVIAMOS EL EMAIL
        if (exito && !string.IsNullOrEmpty(turno.Cliente.Email))
        {
            await _emailService.EnviarCancelacionTurnoAsync(
                turno.Cliente.Email,
                turno.Cliente.Nombre,
                turno.Prestador.Negocio.Nombre,
                inicioSedeLocal, // Le pasamos la hora local para que el email no lo confunda
                turno.Prestador.Negocio.Slug
            );

            // 🎯 MAGIA: ELIMINAMOS EL RECORDATORIO DE HANGFIRE
            if (!string.IsNullOrEmpty(turno.RecordatorioJobId))
            {
                // Llamamos a tu método existente
                _jobService.CancelarTrabajo(turno.RecordatorioJobId);

                turno.RecordatorioJobId = null;
                await _context.SaveChangesAsync();
            }
        }

        return exito;
    }
    public async Task<DashboardResumenDto> GetDashboardResumenAsync()
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        // 1. Obtener la hora ACTUAL en la zona horaria del negocio
        var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede);

        // 2. Determinar los límites en HORA LOCAL
        var inicioMesActualLocal = new DateTime(ahoraLocal.Year, ahoraLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioMesAnteriorLocal = inicioMesActualLocal.AddMonths(-1);
        // 🎯 NUEVO: Límite superior estricto
        var inicioMesSiguienteLocal = inicioMesActualLocal.AddMonths(1);

        // 3. Convertir esos límites locales a UTC
        var inicioMesActualUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesActualLocal, zonaSede);
        var inicioMesAnteriorUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesAnteriorLocal, zonaSede);
        var inicioMesSiguienteUtc = TimeZoneInfo.ConvertTimeToUtc(inicioMesSiguienteLocal, zonaSede);

        // 4. OBTENEMOS LAS RESERVAS 
        var turnos = await _context.Turnos
            .AsNoTracking()
            .Where(t => t.NegocioId == negocioId &&
                        t.FechaHoraInicioUtc >= inicioMesAnteriorUtc &&
                        t.FechaHoraInicioUtc < inicioMesSiguienteUtc && // 🎯 Cortamos el query en BD
                        t.Estado != EstadoTurnoEnum.Cancelado)
            .Select(t => new {
                t.FechaHoraInicioUtc
            })
            .ToListAsync();

        // 🎯 Filtramos en memoria asegurando el cajón del mes actual
        var turnosActual = turnos.Where(t => t.FechaHoraInicioUtc >= inicioMesActualUtc && t.FechaHoraInicioUtc < inicioMesSiguienteUtc).ToList();
        var turnosAnterior = turnos.Where(t => t.FechaHoraInicioUtc >= inicioMesAnteriorUtc && t.FechaHoraInicioUtc < inicioMesActualUtc).ToList();
        
        int reservasActual = turnosActual.Count;
        int reservasAnterior = turnosAnterior.Count;
        // 5. HACEMOS LO MISMO CON LA CAJA
        var ingresosCaja = await _context.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.NegocioId == negocioId &&
                        m.CreatedAtUtc >= inicioMesAnteriorUtc &&
                        m.CreatedAtUtc < inicioMesSiguienteUtc && // 🎯 Acotamos ingresos también
                        m.Tipo == TipoMovimientoEnum.Ingreso)
            .Select(m => new {
                m.CreatedAtUtc,
                m.Monto
            })
            .ToListAsync();

        var ingresosMesActual = ingresosCaja.Where(m => m.CreatedAtUtc >= inicioMesActualUtc && m.CreatedAtUtc < inicioMesSiguienteUtc).ToList();
        var ingresosMesAnterior = ingresosCaja.Where(m => m.CreatedAtUtc >= inicioMesAnteriorUtc && m.CreatedAtUtc < inicioMesActualUtc).ToList();

        decimal ingresosActual = ingresosMesActual.Sum(m => m.Monto);
        decimal ingresosAnterior = ingresosMesAnterior.Sum(m => m.Monto);

        // ==========================================
        // 6. CÁLCULO: DÍA MÁS FUERTE
        // ==========================================
        var diaPicoAgrupado = turnosActual
            // Transformamos a hora local antes de agrupar para que no haya desfasaje de días
            .Select(t => TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, zonaSede))
            .GroupBy(fechaLocal => fechaLocal.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        string diaFuerte = "Sin datos";
        string mensajeDia = "Esperando reservas";

        if (diaPicoAgrupado != null && reservasActual > 0)
        {
            var culturaInfo = new System.Globalization.CultureInfo("es-AR");
            // Traducimos el DayOfWeek a texto (ej: "viernes")
            diaFuerte = culturaInfo.DateTimeFormat.GetDayName(diaPicoAgrupado.Key);
            // Capitalizamos (ej: "Viernes")
            diaFuerte = char.ToUpper(diaFuerte[0]) + diaFuerte.Substring(1);

            // Calculamos qué porcentaje representa ese día sobre el total del mes
            int porcentaje = (int)Math.Round((double)diaPicoAgrupado.Count() / reservasActual * 100);
            mensajeDia = $"{porcentaje}% de tus turnos";
        }

        var cultura = new System.Globalization.CultureInfo("es-AR");

        return new DashboardResumenDto
        {
            // Usamos la fecha local para generar el nombre del mes correcto (Ej: mayo 2026)
            MesActualNombre = inicioMesActualLocal.ToString("MMMM yyyy", cultura).ToLower(),
            MesAnteriorNombre = inicioMesAnteriorLocal.ToString("MMMM yyyy", cultura).ToLower(),
            Ingresos = new MetricaComparativaDto
            {
                ValorActual = ingresosActual,
                ValorAnterior = ingresosAnterior,
                PorcentajeCrecimiento = CalcularPorcentajeCrecimiento(ingresosAnterior, ingresosActual)
            },
            Reservas = new MetricaComparativaDto
            {
                ValorActual = reservasActual,
                ValorAnterior = reservasAnterior,
                PorcentajeCrecimiento = CalcularPorcentajeCrecimiento(reservasAnterior, reservasActual)
            },
            DiaMasFuerte = new TendenciaReservaDto
            {
                Dia = diaFuerte,
                Mensaje = mensajeDia
            }
        };
    }

    private decimal CalcularPorcentajeCrecimiento(decimal anterior, decimal actual)
    {
        if (anterior == 0) return actual > 0 ? 100 : 0;
        return Math.Round(((actual - anterior) / anterior) * 100, 1);
    }

    public async Task<bool> FinalizarYCobrarTurnoAsync(Guid turnoId, MedioPagoEnum medioPago)
    {
        var negocioId = _tenantService.GetCurrentTenantId();

        // 1. Traemos el turno con su Servicio, Prestador, SEDE y Cliente
        var turno = await _context.Turnos
            .Include(t => t.Servicio)
            .Include(t => t.Prestador)
                .ThenInclude(p => p.Sede) // <-- CRUCIAL: Incluimos la sede para leer su zona horaria
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.Id == turnoId && t.NegocioId == negocioId);

        if (turno == null) throw new Exception("Turno no encontrado.");
        if (turno.Estado == EstadoTurnoEnum.Completado) throw new Exception("El turno ya fue cobrado anteriormente.");

        // 2. Extraer la fecha de forma dinámica según la Sede y aplicar el PARCHE para PostgreSQL
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(turno.Prestador!.Sede!.ZonaHorariaId);
        var hoyLocalCrudo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede).Date;

        // LA MAGIA PARA POSTGRESQL:
        var hoyLocal = DateTime.SpecifyKind(hoyLocalCrudo, DateTimeKind.Utc);

        var cajaDelDia = await _context.CajasDiarias
            .FirstOrDefaultAsync(c => c.SedeId == turno.Prestador.SedeId && c.EstaAbierta && c.FechaCaja == hoyLocal);

        // Si el usuario olvidó abrir la caja, se la abrimos automáticamente en cero (auto-apertura)
        if (cajaDelDia == null)
        {
            cajaDelDia = new CajaDiaria
            {
                NegocioId = negocioId.Value,
                SedeId = turno.Prestador.SedeId,
                FechaCaja = hoyLocal, // PostgreSQL ahora lo acepta perfecto
                MontoInicial = 0,
                EstaAbierta = true
            };
            _context.CajasDiarias.Add(cajaDelDia);
            await _context.SaveChangesAsync(); // Guardamos para generar el ID
        }

        // 3. Crear el Movimiento (Congelando el precio histórico)
        var ingreso = new MovimientoCaja
        {
            NegocioId = negocioId.Value,
            CajaDiariaId = cajaDelDia.Id,
            Monto = turno.Servicio.Precio, // ¡AQUÍ CONGELAMOS EL VALOR!
            Tipo = TipoMovimientoEnum.Ingreso,
            MedioPago = medioPago,
            Detalle = $"Cobro Turno: {turno.Servicio.Nombre} - {turno.Cliente.Nombre}",
            TurnoId = turno.Id
        };

        _context.MovimientosCaja.Add(ingreso);

        // 4. Actualizamos el estado del turno
        turno.Estado = EstadoTurnoEnum.Completado;

        // Ejecutamos la transacción en base de datos
        return await _context.SaveChangesAsync() > 0;
    }
    public async Task<byte[]> GenerarReporteExcelAsync(DateTime desde, DateTime hasta)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) throw new UnauthorizedAccessException("No se encontró el negocio.");

        var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        // 🎯 CORRECCIÓN 1: Aseguramos que 'desde' comience a las 00:00:00
        var desdeLocal = DateTime.SpecifyKind(desde.Date, DateTimeKind.Unspecified);

        // 🎯 CORRECCIÓN 2: Extendemos 'hasta' para que abarque hasta las 23:59:59 del día seleccionado
        var hastaLocal = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

        // Convertimos a UTC estricto con los límites correctos
        var desdeUtc = TimeZoneInfo.ConvertTimeToUtc(desdeLocal, zonaSede);
        var hastaUtc = TimeZoneInfo.ConvertTimeToUtc(hastaLocal, zonaSede);

        // Buscamos los turnos usando las fechas UTC precisas
        var turnos = await _context.Turnos
            .AsNoTracking() // Excelente uso de AsNoTracking para reportes de solo lectura
            .Include(t => t.Cliente)
            .Include(t => t.Prestador)
            .Include(t => t.Servicio)
            .Where(t => t.NegocioId == negocioId &&
                        t.FechaHoraInicioUtc >= desdeUtc &&
                        t.FechaHoraInicioUtc <= hastaUtc)
            .OrderBy(t => t.FechaHoraInicioUtc)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Turnos");

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Cell(1, 1).Value = "Fecha";
        worksheet.Cell(1, 2).Value = "Hora";
        worksheet.Cell(1, 3).Value = "Cliente";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Profesional";
        worksheet.Cell(1, 6).Value = "Servicio";
        worksheet.Cell(1, 7).Value = "Estado";
        worksheet.Cell(1, 8).Value = "Precio";

        var currentRow = 2;
        foreach (var t in turnos)
        {
            var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(t.FechaHoraInicioUtc, zonaSede);

            worksheet.Cell(currentRow, 1).Value = fechaLocal.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 2).Value = fechaLocal.ToString("HH:mm");
            worksheet.Cell(currentRow, 3).Value = t.Cliente.Nombre;
            worksheet.Cell(currentRow, 4).Value = t.Cliente.Telefono ?? "";
            worksheet.Cell(currentRow, 5).Value = t.Prestador?.Nombre ?? "";
            worksheet.Cell(currentRow, 6).Value = t.Servicio.Nombre;
            worksheet.Cell(currentRow, 7).Value = t.Estado.ToString();

            var cellPrecio = worksheet.Cell(currentRow, 8);
            cellPrecio.Value = t.Servicio.Precio;
            cellPrecio.Style.NumberFormat.Format = "$ #,##0.00";

            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    public async Task<TurnoReadDto> CrearTurnoAdminAsync(TurnoAdminCreateDto dto)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) throw new UnauthorizedAccessException("Tenant no identificado.");

        var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == dto.ServicioId && s.NegocioId == negocioId);
        if (servicio == null) throw new Exception("El servicio seleccionado no existe.");

        var prestador = await _context.Prestadores
            .Include(p => p.Sede)
            .Include(p => p.Negocio)
            .FirstOrDefaultAsync(p => p.Id == dto.PrestadorId && p.NegocioId == negocioId);

        if (prestador?.Sede == null || prestador?.Negocio == null)
            throw new InvalidOperationException("Prestador o Sede no encontrados.");

        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(prestador.Sede.ZonaHorariaId);

        // Convertimos la fecha local enviada por el admin a UTC
        var fechaLocalCruda = DateTime.SpecifyKind(dto.FechaHoraInicio, DateTimeKind.Unspecified);
        var fechaUtcDefinitiva = TimeZoneInfo.ConvertTimeToUtc(fechaLocalCruda, zonaSede);
        var fechaFinUtcDefinitiva = fechaUtcDefinitiva.AddMinutes(servicio.DuracionMinutos);

        // ==========================================
        // 🛡️ BARRERAS DE VALIDACIÓN (ADMIN MODE)
        // Omitimos Anticipación y Horario Laboral. Solo validamos SUPERPOSICIÓN.
        // ==========================================
        bool turnoOcupado = await _context.Turnos.AnyAsync(t =>
            t.PrestadorId == dto.PrestadorId &&
            t.Estado != EstadoTurnoEnum.Cancelado &&
            (fechaUtcDefinitiva < t.FechaHoraFinUtc && fechaFinUtcDefinitiva > t.FechaHoraInicioUtc)
        );

        if (turnoOcupado)
            throw new InvalidOperationException("Este horario choca con otra reserva existente.");

        bool chocaConBloqueo = await _context.BloqueosAgenda.AnyAsync(b =>
            b.PrestadorId == dto.PrestadorId &&
            b.InicioUtc < fechaFinUtcDefinitiva &&
            fechaUtcDefinitiva < b.FinUtc
        );

        if (chocaConBloqueo)
            throw new InvalidOperationException("Este horario pisa un bloqueo de agenda (vacaciones/ausencia).");

        // ==========================================
        // 👤 GESTIÓN DEL CLIENTE
        // ==========================================
        Cliente clienteAsignado;

        if (dto.ClienteId.HasValue && dto.ClienteId.Value != Guid.Empty)
        {
            // El admin seleccionó un cliente existente del buscador
            clienteAsignado = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.NegocioId == negocioId);
            if (clienteAsignado == null) throw new Exception("El cliente seleccionado no existe.");
        }
        else
        {
            // Es un cliente nuevo creado "al vuelo"
            if (string.IsNullOrWhiteSpace(dto.NuevoClienteNombre))
                throw new InvalidOperationException("Debe ingresar el nombre del cliente.");

            clienteAsignado = new Cliente
            {
                Id = Guid.CreateVersion7(),
                NegocioId = negocioId.Value,
                Nombre = dto.NuevoClienteNombre.Trim(),
                Email = dto.NuevoClienteEmail?.Trim().ToLower(),
                Telefono = dto.NuevoClienteTelefono?.Trim()
            };
            _context.Clientes.Add(clienteAsignado);
        }

        // ==========================================
        // 📅 CREACIÓN DEL TURNO (Unit of Work)
        // ==========================================
        var turnoId = Guid.CreateVersion7();
        var fechaRecordatorioUtc = fechaUtcDefinitiva.AddHours(-1);
        string? recordatorioJobId = null;

        // Solo programamos Hangfire si hay email y el turno es a futuro
        if (!string.IsNullOrWhiteSpace(clienteAsignado.Email) && fechaRecordatorioUtc > DateTime.UtcNow)
        {
            recordatorioJobId = _jobService.ProgramarRecordatorioEmail(
                clienteAsignado.Email,
                clienteAsignado.Nombre,
                prestador.Negocio.Nombre,
                fechaLocalCruda, // Pasamos la local para el texto del mail
                fechaRecordatorioUtc,
                turnoId
            );
        }

        var nuevoTurno = new Turno
        {
            Id = turnoId,
            NegocioId = negocioId.Value,
            PrestadorId = prestador.Id,
            FechaHoraInicioUtc = fechaUtcDefinitiva,
            FechaHoraFinUtc = fechaFinUtcDefinitiva,
            ClienteId = clienteAsignado.Id,
            Estado = EstadoTurnoEnum.Confirmado,
            ServicioId = dto.ServicioId,
            RecordatorioJobId = recordatorioJobId
        };

        _context.Turnos.Add(nuevoTurno);
        await _context.SaveChangesAsync();

        // ==========================================
        // 📧 ENVÍO DE EMAIL (Con resiliencia)
        // ==========================================
        if (!string.IsNullOrWhiteSpace(clienteAsignado.Email))
        {
            try
            {
                await _emailService.EnviarConfirmacionTurnoAsync(
                    clienteAsignado.Email,
                    clienteAsignado.Nombre,
                    prestador.Negocio.Nombre,
                    fechaLocalCruda, // Hora local para el correo
                    nuevoTurno.Id,
                    servicio.Nombre,
                    prestador.Nombre,
                    prestador.Sede.Nombre,
                    prestador.Sede.Direccion
                );
            }
            catch (Exception)
            {
                // El error de email no rompe la reserva manual
            }
        }

        return _mapper.Map<TurnoReadDto>(nuevoTurno);
    }
}