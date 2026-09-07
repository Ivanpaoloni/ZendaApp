using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.DTOs.Admin;
using Zenda.Core.Entities;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

public class NegocioService : INegocioService
{
    private readonly IZendaDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantService _tenantService;
    private readonly IPlanService _planService;

    public NegocioService(IZendaDbContext context, IMapper mapper, ITenantService tenantService, IPlanService planService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
        _planService = planService;
    }

    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        if (tenantId == null)
            return null;

        // 1. Acceso a Datos: Extraemos el negocio con su historial sin filtrar por fechas en BD.
        // Garantiza que los planes gratuitos antiguos se carguen en memoria.
        var negocio = await _context.Negocios
            .Include(n => n.Sedes)
            .Include(n => n.Suscripciones)
                .ThenInclude(s => s.PlanSuscripcion)
            .FirstOrDefaultAsync(n => n.Id == tenantId);

        if (negocio == null)
            return null;

        if (!negocio.IsActive)
        {
            throw new UnauthorizedAccessException("CUENTA_SUSPENDIDA");
        }

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        // 2. Obtenemos la última suscripción cronológicamente
        var ultimaSuscripcion = negocio.Suscripciones
            .OrderByDescending(s => s.FechaVencimiento)
            .FirstOrDefault();

        if (ultimaSuscripcion?.PlanSuscripcion != null)
        {
            dto.PlanNombre = ultimaSuscripcion.PlanSuscripcion.Nombre;
            dto.PlanSuscripcionFechaVencimiento = ultimaSuscripcion.FechaVencimiento;
            dto.PlanSuscripcionPrecioMensual = ultimaSuscripcion.PlanSuscripcion.PrecioMensual;
            dto.PlanSuscripcionId = ultimaSuscripcion.PlanSuscripcion.Id;
            dto.MaxProfesionales = ultimaSuscripcion.PlanSuscripcion.MaxProfesionales;
            dto.MaxSedes = ultimaSuscripcion.PlanSuscripcion.MaxSedes;

            // 3. Dominio Rico: El dominio evalúa el PrecioMensual == 0 y los días de gracia
            dto.EsSuscripcionActiva = ultimaSuscripcion.EsSuscripcionActiva;
            dto.EsPeriodoDeGracia = ultimaSuscripcion.EsPeriodoDeGracia;
        }
        else
        {
            // Fallback defensivo por si la base de datos está inconsistente
            dto.EsSuscripcionActiva = false;
            dto.EsPeriodoDeGracia = false;
        }

        return dto;
    }

    public async Task<NegocioReadDto?> GetPublicBySlugAsync(string slug)
    {
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == slug);

        if (negocio == null) return null;

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        var usuarioDueño = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NegocioId == negocio.Id);

        if (usuarioDueño != null)
        {
            dto.Telefono = usuarioDueño.PhoneNumber;
        }

        return dto;
    }

    public async Task<NegocioReadDto?> GetByIdAsync(Guid id)
    {
        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        return _mapper.Map<NegocioReadDto>(negocio);
    }

    public async Task<NegocioReadDto> CreateAsync(NegocioCreateDto dto)
    {
        var planInicial = await _context.PlanesSuscripcion
            .FirstOrDefaultAsync(p => p.Nombre == "Single" || p.PrecioMensual == 0)
            ?? throw new InvalidOperationException("No se encontró un plan inicial activo configurado en el sistema.");

        // VALIDACIÓN ARQUITECTÓNICA: ¿Existe ya una transacción gestionada por un Application Service superior (ej. AuthService)?
        var isOwnTransaction = _context.Database.CurrentTransaction == null;
        var transaction = isOwnTransaction ? await _context.Database.BeginTransactionAsync() : null;

        try
        {
            var negocio = _mapper.Map<Negocio>(dto);
            negocio.Id = Guid.CreateVersion7();
            negocio.Slug = dto.Slug.ToLower().Replace(" ", "-").Trim();
            negocio.CreatedAtUtc = DateTime.UtcNow;
            negocio.IsActive = true;

            _context.Negocios.Add(negocio);
            await _context.SaveChangesAsync();

            var esGratuito = planInicial.PrecioMensual == 0;
            var fechaInicio = DateTime.UtcNow;
            var fechaVencimiento = esGratuito ? fechaInicio.AddYears(1) : fechaInicio.AddDays(14);

            var suscripcionInicial = new SuscripcionNegocio
            {
                Id = Guid.CreateVersion7(),
                NegocioId = negocio.Id,
                PlanSuscripcionId = planInicial.Id,
                Estado = EstadoSuscripcionEnum.Activa,
                FechaInicio = fechaInicio,
                FechaVencimiento = fechaVencimiento,
                CreatedAtUtc = fechaInicio,
                PrecioMensualPersonalizado = esGratuito ? 0 : null
            };

            _context.SuscripcionesNegocio.Add(suscripcionInicial);
            await _context.SaveChangesAsync();

            if (isOwnTransaction && transaction != null)
            {
                await transaction.CommitAsync();
            }

            var resultDto = _mapper.Map<NegocioReadDto>(negocio);
            resultDto.PlanNombre = planInicial.Nombre;
            resultDto.EsSuscripcionActiva = true;
            resultDto.EsPeriodoDeGracia = false;

            return resultDto;
        }
        catch
        {
            if (isOwnTransaction && transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    public async Task<bool> IsSlugAvailableAsync(string slug)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // 🛡️ REGLA DE ORO: Ignoramos los filtros porque los slugs son únicos globales.
        // Pero excluimos MI PROPIO negocio, porque si el slug es el que ya tengo, ¡obvio que está disponible para mí!
        var slugOcupado = await _context.Negocios
            .IgnoreQueryFilters()
            .AnyAsync(n => n.Slug.ToLower() == slug.ToLower() && n.Id != tenantId);

        return !slugOcupado; // Si NO está ocupado, está disponible (true)
    }

    public async Task<bool> UpdatePerfilAsync(NegocioUpdateDto dto)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == tenantId);
        if (negocio == null) return false;

        if (!await IsSlugAvailableAsync(dto.Slug))
            throw new InvalidOperationException("El link elegido ya está en uso por otra empresa.");

        negocio.Nombre = dto.Nombre;
        negocio.IntervaloTurnosMinutos = dto.IntervaloTurnosMinutos;
        negocio.AnticipacionMinimaHoras = dto.AnticipacionMinimaHoras;
        negocio.RubroId = dto.RubroId;
        negocio.Slug = dto.Slug.ToLower().Replace(" ", "-").Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateLogoUrlAsync(string logoUrl)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var negocio = await _context.Negocios.FirstOrDefaultAsync(n => n.Id == tenantId);
        if (negocio == null) return false;

        negocio.LogoUrl = logoUrl;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CambiarAPlanGratuitoAsync(Guid planId)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (tenantId == null) return false;

        var planGratuito = await _context.PlanesSuscripcion.FindAsync(planId);
        if (planGratuito == null || planGratuito.PrecioMensual > 0)
            return false;

        var sedesActivas = await _context.Sedes.CountAsync(s => s.NegocioId == tenantId);
        var profesionalesActivos = await _context.Prestadores.CountAsync(p => p.NegocioId == tenantId);

        if (sedesActivas > planGratuito.MaxSedes || profesionalesActivos > planGratuito.MaxProfesionales)
        {
            throw new InvalidOperationException($"El uso actual supera los límites del plan {planGratuito.Nombre}. Ajustá tu negocio primero.");
        }

        var suscripcionActual = await _context.SuscripcionesNegocio
            .OrderByDescending(s => s.FechaVencimiento)
            .FirstOrDefaultAsync(s => s.NegocioId == tenantId);

        if (suscripcionActual != null)
        {
            suscripcionActual.PlanSuscripcionId = planGratuito.Id;
            // Extendemos la fecha nominalmente; el dominio igualmente la validará como activa al ser $0
            suscripcionActual.FechaVencimiento = DateTime.UtcNow.AddYears(1);
        }
        else
        {
            var nuevaSuscripcion = new SuscripcionNegocio
            {
                NegocioId = tenantId.Value,
                PlanSuscripcionId = planGratuito.Id,
                Estado = EstadoSuscripcionEnum.Activa,
                FechaInicio = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddYears(1)
            };
            _context.SuscripcionesNegocio.Add(nuevaSuscripcion);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarSuscripcionAdminAsync(Guid negocioId, AdminUpdateNegocioDto dto)
    {
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == negocioId);

        if (negocio == null) return false;

        var suscripcion = await _context.SuscripcionesNegocio
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

        if (suscripcion == null)
        {
            suscripcion = new SuscripcionNegocio
            {
                Id = Guid.NewGuid(),
                NegocioId = negocioId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.SuscripcionesNegocio.Add(suscripcion);
        }

        // --- LÓGICA DE DETECCIÓN DE CAMBIOS ---

        decimal? nuevoPrecio = dto.PrecioMensualPersonalizado;
        var nuevaFechaVencimientoUtc = dto.FechaVencimiento.ToUniversalTime();

        // Validaciones de cambio de estado
        bool huboCambioPrecio = suscripcion.PrecioMensualPersonalizado != nuevoPrecio;
        bool huboCambioPlan = suscripcion.PlanSuscripcionId != dto.PlanSuscripcionId;
        bool huboCambioFecha = suscripcion.FechaVencimiento.Date != nuevaFechaVencimientoUtc.Date;
        bool esCeroExplicito = nuevoPrecio.HasValue && nuevoPrecio.Value == 0;

        // Solo generamos historial si hubo un cambio real en el contrato
        bool generarHistorial = huboCambioPlan || huboCambioFecha || huboCambioPrecio;

        // --- ACTUALIZACIÓN DE ENTIDADES ---
        negocio.IsActive = dto.IsActive;

        suscripcion.PlanSuscripcionId = dto.PlanSuscripcionId;
        suscripcion.PrecioMensualPersonalizado = nuevoPrecio;
        suscripcion.FechaVencimiento = nuevaFechaVencimientoUtc;
        suscripcion.Estado = EstadoSuscripcionEnum.Activa;

        if (generarHistorial)
        {
            var historial = new HistorialPago
            {
                SuscripcionNegocio = suscripcion,
                MontoCobrado = nuevoPrecio ?? 0, // Si es nulo, el monto registrado es 0
                FechaPago = DateTime.UtcNow,
                MercadoPagoPaymentId = esCeroExplicito ? "BONIFICACION_ADMIN" : "AJUSTE_ADMIN",
                DetalleRecibo = esCeroExplicito
                    ? "Suscripción bonificada al 100% por administración."
                    : $"Ajuste de suscripción. Precio mensual: {nuevoPrecio?.ToString("C") ?? "Precio de Lista"}."
            };
            _context.HistorialPagos.Add(historial);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    internal async Task<Negocio> GetAsync(Guid id)
    {
        var entity = await FindAsync(id);

        if (entity is null)
            throw new Exception("Negocio no encontrado");

        return entity;
    }

    internal async Task<Negocio?> FindAsync(Guid id)
    {
        var entity = await _context.Negocios.FindAsync(id);
        return entity;
    }
}