namespace Zenda.Core.DTOs;

public class ReporteDashboardDto
{
    // MÉTRICAS DE CAJA
    public decimal IngresosTotales { get; set; }
    public int CantidadTurnosCobrados { get; set; }
    public decimal TicketPromedio => CantidadTurnosCobrados > 0 ? IngresosTotales / CantidadTurnosCobrados : 0;

    // MÉTRICAS DE TURNOS
    public int TurnosConfirmados { get; set; }
    public int TurnosCompletados { get; set; }
    public int TurnosCancelados { get; set; }
    public int TurnosAusentes { get; set; }
    public int TotalTurnos => TurnosConfirmados + TurnosCompletados + TurnosCancelados + TurnosAusentes;

    // Tasa de Ausentismo (No-show rate)
    public decimal PorcentajeAusentismo => TotalTurnos > 0 ? Math.Round((decimal)TurnosAusentes / TotalTurnos * 100, 2) : 0;

    // GRÁFICOS
    public List<DatoGraficoDto> IngresosPorMedioPago { get; set; } = new();
    public List<DatoGraficoDto> IngresosPorPrestador { get; set; } = new();
    public List<DatoGraficoDto> TopServicios { get; set; } = new();
}

public class DatoGraficoDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}