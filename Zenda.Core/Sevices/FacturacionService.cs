using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Sevices
{
    public class FacturacionService : IFacturacionService
    {
        private readonly IZendaDbContext _context;
        private readonly ITenantService _tenantService;

        public FacturacionService(IZendaDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<FacturacionDto?> GetResumenAsync()
        {
            var negocioId = _tenantService.GetCurrentTenantId();
            if (negocioId == null) return null;

            var negocio = await _context.Negocios
                .Include(n => n.Sedes)
                .FirstOrDefaultAsync(n => n.Id == negocioId);

            if (negocio == null) return null;

            var suscripcion = await _context.SuscripcionesNegocio
                .Include(s => s.PlanSuscripcion)
                .FirstOrDefaultAsync(s => s.NegocioId == negocioId);

            // 2. Calculamos los profesionales usados (SOLO LOS ACTIVOS)
            var profesionalesUsados = await _context.Prestadores
                // Asumiendo que agregamos una propiedad 'IsDeleted' o 'Activo' a la entidad
                .CountAsync(p => p.NegocioId == negocioId && !p.IsDeleted);

            var historial = await _context.HistorialPagos
                .Include(h => h.SuscripcionNegocio)
                .ThenInclude(s => s.PlanSuscripcion)
                .Where(h => h.SuscripcionNegocio.NegocioId == negocioId)
                .OrderByDescending(h => h.FechaPago)
                .Take(10)
                .Select(h => new HistorialPagoDto
                {
                    Fecha = h.FechaPago,
                    Monto = h.MontoCobrado,
                    PlanNombre = h.SuscripcionNegocio.PlanSuscripcion.Nombre,
                    TransaccionId = h.MercadoPagoPaymentId
                })
                .ToListAsync();

            return new FacturacionDto
            {
                PlanActualNombre = suscripcion?.PlanSuscripcion?.Nombre ?? "Single",
                Estado = suscripcion?.Estado.ToString() ?? "Activa",
                FechaVencimiento = suscripcion?.FechaVencimiento ?? DateTime.UtcNow.AddYears(1),
                SedesUsadas = negocio.Sedes.Count(),
                SedesMaximas = suscripcion?.PlanSuscripcion?.MaxSedes ?? 1,
                ProfesionalesUsados = profesionalesUsados,
                ProfesionalesMaximos = suscripcion?.PlanSuscripcion?.MaxProfesionales ?? 1,
                Pagos = historial
            };
        }
    }
}