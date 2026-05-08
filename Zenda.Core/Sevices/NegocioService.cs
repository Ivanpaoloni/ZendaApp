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

    public NegocioService(IZendaDbContext context, IMapper mapper, ITenantService tenantService)
    {
        _context = context;
        _mapper = mapper;
        _tenantService = tenantService;
    }

    public async Task<NegocioReadDto?> GetPerfilAsync()
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        if (tenantId == null)
            return null;

        var negocio = await _context.Negocios
            .Include(n => n.Sedes)
            // 🔥 EL FIX 1: Cambiamos .Include(n => n.PlanSuscripcion) 
            // por un Include anidado a través del historial de suscripciones.
            // Solo traemos la suscripción activa para no saturar la memoria.
            .Include(n => n.Suscripciones.Where(s => s.Estado == EstadoSuscripcionEnum.Activa))
                .ThenInclude(s => s.PlanSuscripcion)
            .FirstOrDefaultAsync(n => n.Id == tenantId);

        if (negocio == null)
            return null;

        if (!negocio.IsActive)
        {
            throw new UnauthorizedAccessException("CUENTA_SUSPENDIDA");
        }

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        // 🔥 EL FIX 2: Buscamos el plan activo a través del método auxiliar que definimos en la entidad
        var suscripcionActiva = negocio.ObtenerSuscripcionActiva();

        if (suscripcionActiva?.PlanSuscripcion != null)
        {
            dto.PlanNombre = suscripcionActiva.PlanSuscripcion.Nombre;
            dto.PlanSuscripcionId = suscripcionActiva.PlanSuscripcion.Id; // Usamos el ID del plan, no de la suscripción
            dto.MaxProfesionales = suscripcionActiva.PlanSuscripcion.MaxProfesionales;
            dto.MaxSedes = suscripcionActiva.PlanSuscripcion.MaxSedes;
        }

        return dto;
    }

    public async Task<NegocioReadDto?> GetPublicBySlugAsync(string slug)
    {
        // 1. Consulta pública del negocio
        var negocio = await _context.Negocios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == slug);

        if (negocio == null) return null;

        var dto = _mapper.Map<NegocioReadDto>(negocio);

        // 2. 🎯 MAGIA DE IDENTITY: Usamos _context.Users (disponible gracias a IdentityDbContext)
        var usuarioDueño = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NegocioId == negocio.Id);

        if (usuarioDueño != null)
        {
            // Identity usa "PhoneNumber" de forma nativa
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
        var negocio = _mapper.Map<Negocio>(dto);
        negocio.Id = Guid.CreateVersion7(); // O GuidV7
        negocio.Slug = dto.Slug.ToLower().Replace(" ", "-");
        negocio.CreatedAtUtc = DateTime.UtcNow;

        _context.Negocios.Add(negocio);
        await _context.SaveChangesAsync();

        return _mapper.Map<NegocioReadDto>(negocio);
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

        // Doble validación por seguridad (por si alguien saltea el frontend)
        if (!await IsSlugAvailableAsync(dto.Slug))
            throw new InvalidOperationException("El link elegido ya está en uso por otra empresa.");

        negocio.Nombre = dto.Nombre;
        negocio.IntervaloTurnosMinutos = dto.IntervaloTurnosMinutos;
        negocio.AnticipacionMinimaHoras = dto.AnticipacionMinimaHoras;
        negocio.RubroId = dto.RubroId;
        // Limpiamos el slug por si nos mandan mayúsculas o espacios raros
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

        // 1. Obtener el plan gratuito y validar que realmente sea gratis por seguridad
        var planGratuito = await _context.PlanesSuscripcion.FindAsync(planId);
        if (planGratuito == null || planGratuito.PrecioMensual > 0)
            return false;

        // 2. Validación Estricta: Contar uso real
        var sedesActivas = await _context.Sedes.CountAsync(s => s.NegocioId == tenantId);
        var profesionalesActivos = await _context.Prestadores.CountAsync(p => p.NegocioId == tenantId);

        if (sedesActivas > planGratuito.MaxSedes || profesionalesActivos > planGratuito.MaxProfesionales)
        {
            throw new InvalidOperationException($"El uso actual supera los límites del plan {planGratuito.Nombre}. Ajustá tu negocio primero.");
        }

        // 3. Modificar la Suscripción 
        // 🔥 EL FIX 3: Ya no modificamos el Negocio, solo la SuscripcionNegocio.
        var suscripcionActual = await _context.SuscripcionesNegocio
            .FirstOrDefaultAsync(s => s.NegocioId == tenantId && s.Estado == EstadoSuscripcionEnum.Activa);

        if (suscripcionActual != null)
        {
            // Opción A: Actualizar la suscripción existente (ideal para planes gratis)
            suscripcionActual.PlanSuscripcionId = planGratuito.Id;
            suscripcionActual.FechaVencimiento = DateTime.UtcNow.AddYears(1);

            // Opción B (Más estricta para auditoría): 
            // suscripcionActual.Estado = EstadoSuscripcionEnum.Cancelada;
            // Luego crearías un new SuscripcionNegocio con el plan gratis.
        }
        else
        {
            // Fallback por si la base de datos estaba inconsistente y no tenía suscripción
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
            .FirstOrDefaultAsync(s => s.NegocioId == negocioId && s.Estado == EstadoSuscripcionEnum.Activa);

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
}