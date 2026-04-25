using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces;

public interface IReporteService
{
    /// <summary>
    /// Obtiene las métricas generales de un negocio en un rango de fechas.
    /// Las fechas deben venir en formato UTC.
    /// </summary>
    Task<ReporteDashboardDto> GetDashboardMetricsAsync(DateTime fechaInicioUtc, DateTime fechaFinUtc); 
    Task<byte[]> GenerarReporteExcelAsync(DateTime fechaInicioUtc, DateTime fechaFinUtc);
}