using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Sevices
{
    public class CajaService : ICajaService
    {
        private readonly IZendaDbContext _context;
        private readonly ITenantService _tenantService;

        public CajaService(IZendaDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<CajaDiariaDto?> GetEstadoCajaHoyAsync(Guid sedeId)
        {
            var negocioId = _tenantService.GetCurrentTenantId();

            // 1. Buscamos la sede para saber su zona horaria
            var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == sedeId && s.NegocioId == negocioId);
            if (sede == null) throw new Exception("Sede no encontrada.");

            // 2. Calculamos "Hoy" y aplicamos el parche para PostgreSQL
            var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(sede.ZonaHorariaId);
            var hoyLocalCrudo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede).Date;
            var hoyLocal = DateTime.SpecifyKind(hoyLocalCrudo, DateTimeKind.Utc);

            // 3. Buscamos la caja del día
            var caja = await _context.CajasDiarias
                .Include(c => c.Movimientos)
                .FirstOrDefaultAsync(c => c.SedeId == sedeId && c.FechaCaja == hoyLocal && c.NegocioId == negocioId);

            if (caja == null) return null;

            var ingresos = caja.Movimientos.Where(m => m.Tipo == TipoMovimientoEnum.Ingreso).Sum(m => m.Monto);
            var egresos = caja.Movimientos.Where(m => m.Tipo == TipoMovimientoEnum.Egreso).Sum(m => m.Monto);

            return new CajaDiariaDto
            {
                Id = caja.Id,
                FechaCaja = caja.FechaCaja,
                MontoInicial = caja.MontoInicial,
                EstaAbierta = caja.EstaAbierta,
                TotalIngresos = ingresos,
                TotalEgresos = egresos,
                SaldoActual = caja.MontoInicial + ingresos - egresos,
                Movimientos = caja.Movimientos.OrderByDescending(m => m.CreatedAtUtc).Select(m => new MovimientoCajaDto
                {
                    Id = m.Id,
                    Monto = m.Monto,
                    Tipo = m.Tipo,
                    MedioPago = m.MedioPago,
                    Detalle = m.Detalle,
                    CreatedAtUtc = m.CreatedAtUtc
                }).ToList()
            };
        }

        public async Task<bool> AbrirCajaAsync(Guid sedeId, decimal montoInicial)
        {
            var negocioId = _tenantService.GetCurrentTenantId();

            // 1. Buscamos la sede para saber su zona horaria
            var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == sedeId && s.NegocioId == negocioId);
            if (sede == null) throw new Exception("Sede no encontrada.");

            // 2. Calculamos "Hoy" y aplicamos el parche para PostgreSQL
            var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(sede.ZonaHorariaId);
            var hoyLocalCrudo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede).Date;
            var hoyLocal = DateTime.SpecifyKind(hoyLocalCrudo, DateTimeKind.Utc);

            bool yaExiste = await _context.CajasDiarias.AnyAsync(c => c.SedeId == sedeId && c.FechaCaja == hoyLocal && c.NegocioId == negocioId);
            if (yaExiste) throw new Exception("La caja ya fue abierta para el día de hoy.");

            var nuevaCaja = new CajaDiaria
            {
                NegocioId = negocioId.Value,
                SedeId = sedeId,
                FechaCaja = hoyLocal, // Guardamos la fecha con Kind = Utc
                MontoInicial = montoInicial,
                EstaAbierta = true
            };

            _context.CajasDiarias.Add(nuevaCaja);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RegistrarMovimientoManualAsync(NuevoMovimientoDto dto)
        {
            var negocioId = _tenantService.GetCurrentTenantId();

            // 1. Buscamos la sede para saber su zona horaria
            var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == dto.SedeId && s.NegocioId == negocioId);
            if (sede == null) throw new Exception("Sede no encontrada.");

            // 2. Calculamos "Hoy" y aplicamos el parche para PostgreSQL
            var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(sede.ZonaHorariaId);
            var hoyLocalCrudo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaSede).Date;
            var hoyLocal = DateTime.SpecifyKind(hoyLocalCrudo, DateTimeKind.Utc);

            var caja = await _context.CajasDiarias
                .FirstOrDefaultAsync(c => c.SedeId == dto.SedeId && c.FechaCaja == hoyLocal && c.EstaAbierta && c.NegocioId == negocioId);

            if (caja == null) throw new Exception("Debe abrir la caja del día antes de registrar gastos o ingresos adicionales.");

            var movimiento = new MovimientoCaja
            {
                NegocioId = negocioId.Value,
                CajaDiariaId = caja.Id,
                Monto = dto.Monto,
                Tipo = dto.Tipo,
                MedioPago = dto.MedioPago,
                Detalle = dto.Detalle
            };

            _context.MovimientosCaja.Add(movimiento);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}