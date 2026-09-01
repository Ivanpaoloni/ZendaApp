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

            var profesionalesUsados = await _context.Prestadores
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

            // 🎯 Lógica de estados de vencimiento
            var fechaVencimiento = suscripcion?.FechaVencimiento ?? DateTime.UtcNow;
            var hoy = DateTime.UtcNow;

            // Consideramos "próximo a vencer" si quedan 7 días o menos
            bool estaVencido = fechaVencimiento < hoy;
            bool proximoAVencer = estaVencido || (fechaVencimiento - hoy).TotalDays <= 7;

            string estado = estaVencido ? "Vencido" : (proximoAVencer ? "Por Vencer" : "Activa");

            return new FacturacionDto
            {
                PlanActualId = suscripcion?.PlanSuscripcionId ?? Guid.Empty,
                PlanActualNombre = suscripcion?.PlanSuscripcion?.Nombre ?? "Single",
                PlanActualPrecio = suscripcion?.PlanSuscripcion?.PrecioMensual ?? 0m,
                Estado = estado,
                FechaVencimiento = fechaVencimiento,
                ProximoAVencer = proximoAVencer,
                SedesUsadas = negocio.Sedes.Count,
                SedesMaximas = suscripcion?.PlanSuscripcion?.MaxSedes ?? 1,
                ProfesionalesUsados = profesionalesUsados,
                ProfesionalesMaximos = suscripcion?.PlanSuscripcion?.MaxProfesionales ?? 1,
                Pagos = historial
            };
        }
    }
}