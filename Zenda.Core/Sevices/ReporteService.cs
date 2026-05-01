using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Zenda.Core.DTOs;
using Zenda.Core.Enums;
using Zenda.Core.Interfaces;

namespace Zenda.Core.Services;

public class ReporteService : IReporteService
{
    private readonly IZendaDbContext _context;
    private readonly ITenantService _tenantService;

    public ReporteService(IZendaDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ReporteDashboardDto> GetDashboardMetricsAsync(DateTime fechaInicioUtc, DateTime fechaFinUtc)
    {
        var negocioId = _tenantService.GetCurrentTenantId();
        if (negocioId == null) throw new UnauthorizedAccessException("Tenant no identificado.");

        // =========================================================
        // 🔥 FIX: TimeZone Shift y Date Boundaries (Alinear con el Home)
        // =========================================================
        var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.NegocioId == negocioId);
        var zonaHorariaId = sede?.ZonaHorariaId ?? "America/Argentina/Buenos_Aires";
        var zonaSede = TimeZoneInfo.FindSystemTimeZoneById(zonaHorariaId);

        // 1. Tomamos las fechas puras del frontend y las forzamos a locales
        var inicioLocal = DateTime.SpecifyKind(fechaInicioUtc.Date, DateTimeKind.Unspecified);

        // 2. MAGIA: Extendemos el límite superior para abarcar todo el último día hasta el último tick
        var finLocal = DateTime.SpecifyKind(fechaFinUtc.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

        // 3. Convertimos a UTC estricto para hacer la consulta segura en BD
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, zonaSede);
        var finUtc = TimeZoneInfo.ConvertTimeToUtc(finLocal, zonaSede);

        var reporte = new ReporteDashboardDto();

        // ==========================================
        // 1. MÉTRICAS DE TURNOS (Directo a SQL)
        // ==========================================
        var turnosAgrupados = await _context.Turnos
            .Where(t => t.FechaHoraInicioUtc >= inicioUtc && t.FechaHoraInicioUtc <= finUtc) // Usamos las fechas UTC corregidas
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        reporte.TurnosConfirmados = turnosAgrupados.FirstOrDefault(x => x.Estado == EstadoTurnoEnum.Confirmado)?.Cantidad ?? 0;
        reporte.TurnosCompletados = turnosAgrupados.FirstOrDefault(x => x.Estado == EstadoTurnoEnum.Completado)?.Cantidad ?? 0;
        reporte.TurnosCancelados = turnosAgrupados.FirstOrDefault(x => x.Estado == EstadoTurnoEnum.Cancelado)?.Cantidad ?? 0;
        reporte.TurnosAusentes = turnosAgrupados.FirstOrDefault(x => x.Estado == EstadoTurnoEnum.Ausente)?.Cantidad ?? 0;

        // ==========================================
        // 2. MÉTRICAS DE CAJA Y FINANZAS
        // ==========================================
        var queryIngresos = _context.MovimientosCaja
            .Where(m => m.CreatedAtUtc >= inicioUtc
                     && m.CreatedAtUtc <= finUtc
                     && m.Tipo == TipoMovimientoEnum.Ingreso);

        var datosIngresos = await queryIngresos
            .Select(m => new
            {
                m.Monto,
                m.MedioPago,
                EsTurno = m.TurnoId != null,
                PrestadorNombre = m.Turno != null ? m.Turno.Prestador!.Nombre : "Otros",
                ServicioNombre = m.Turno != null ? m.Turno.Servicio.Nombre : "Venta General"
            })
            .ToListAsync();

        // Cálculos generales
        reporte.IngresosTotales = datosIngresos.Sum(x => x.Monto);
        reporte.CantidadTurnosCobrados = datosIngresos.Count(x => x.EsTurno);

        // ==========================================
        // 3. ARMADO DE GRÁFICOS (Agrupaciones en memoria)
        // ==========================================
        reporte.IngresosPorMedioPago = datosIngresos
            .GroupBy(x => x.MedioPago)
            .Select(g => new DatoGraficoDto { Etiqueta = g.Key.ToString(), Valor = g.Sum(x => x.Monto) })
            .ToList();

        reporte.IngresosPorPrestador = datosIngresos
            .Where(x => x.EsTurno)
            .GroupBy(x => x.PrestadorNombre)
            .Select(g => new DatoGraficoDto { Etiqueta = g.Key, Valor = g.Sum(x => x.Monto) })
            .OrderByDescending(x => x.Valor)
            .ToList();

        reporte.TopServicios = datosIngresos
            .Where(x => x.EsTurno)
            .GroupBy(x => x.ServicioNombre)
            .Select(g => new DatoGraficoDto { Etiqueta = g.Key, Valor = g.Count() })
            .OrderByDescending(x => x.Valor)
            .Take(5)
            .ToList();

        return reporte;
    }

    public async Task<byte[]> GenerarReporteExcelAsync(DateTime fechaInicioUtc, DateTime fechaFinUtc)
    {
        // 1. Obtenemos las métricas exactas que ve en pantalla
        var metricas = await GetDashboardMetricsAsync(fechaInicioUtc, fechaFinUtc);

        // 2. Armamos el Excel en memoria
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Métricas y Caja");

        // --- Estilos Generales ---
        var titleRow = worksheet.Row(1);
        titleRow.Style.Font.Bold = true;
        titleRow.Style.Font.FontSize = 14;
        worksheet.Cell(1, 1).Value = "REPORTE DE RENDIMIENTO ZENDY";
        worksheet.Cell(2, 1).Value = $"Período evaluado: {fechaInicioUtc:dd/MM/yyyy} al {fechaFinUtc:dd/MM/yyyy}";

        // --- SECCIÓN 1: RESUMEN GENERAL ---
        worksheet.Cell(4, 1).Value = "RESUMEN GENERAL";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(4, 1, 4, 2).Merge();

        worksheet.Cell(5, 1).Value = "Ingresos Totales";
        worksheet.Cell(5, 2).Value = metricas.IngresosTotales;
        worksheet.Cell(5, 2).Style.NumberFormat.Format = "$ #,##0.00";

        worksheet.Cell(6, 1).Value = "Ticket Promedio";
        worksheet.Cell(6, 2).Value = metricas.TicketPromedio;
        worksheet.Cell(6, 2).Style.NumberFormat.Format = "$ #,##0.00";

        worksheet.Cell(7, 1).Value = "Turnos Confirmados";
        worksheet.Cell(7, 2).Value = metricas.TurnosConfirmados;

        worksheet.Cell(8, 1).Value = "Turnos Completados";
        worksheet.Cell(8, 2).Value = metricas.TurnosCompletados;

        worksheet.Cell(9, 1).Value = "Turnos Ausentes";
        worksheet.Cell(9, 2).Value = metricas.TurnosAusentes;

        // --- SECCIÓN 2: FACTURACIÓN POR PROFESIONAL ---
        worksheet.Cell(11, 1).Value = "FACTURACIÓN POR PROFESIONAL";
        worksheet.Cell(11, 1).Style.Font.Bold = true;
        worksheet.Cell(11, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(11, 1, 11, 2).Merge();

        worksheet.Cell(12, 1).Value = "Profesional";
        worksheet.Cell(12, 2).Value = "Ingresos Generados";
        worksheet.Range(12, 1, 12, 2).Style.Font.Bold = true;

        int currentRow = 13;
        foreach (var p in metricas.IngresosPorPrestador)
        {
            worksheet.Cell(currentRow, 1).Value = p.Etiqueta;
            worksheet.Cell(currentRow, 2).Value = p.Valor;
            worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "$ #,##0.00";
            currentRow++;
        }

        // --- SECCIÓN 3: SERVICIOS MÁS SOLICITADOS ---
        currentRow++;
        worksheet.Cell(currentRow, 1).Value = "SERVICIOS MÁS SOLICITADOS";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(currentRow, 1, currentRow, 2).Merge();

        currentRow++;
        worksheet.Cell(currentRow, 1).Value = "Servicio";
        worksheet.Cell(currentRow, 2).Value = "Cantidad Realizada";
        worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;

        currentRow++;
        foreach (var s in metricas.TopServicios)
        {
            worksheet.Cell(currentRow, 1).Value = s.Etiqueta;
            worksheet.Cell(currentRow, 2).Value = s.Valor;
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();

        // 3. Convertimos a Stream y devolvemos los bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}