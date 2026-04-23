namespace Zenda.Core.DTOs;

public class DashboardResumenDto
{
    public string MesActualNombre { get; set; } = string.Empty;
    public string MesAnteriorNombre { get; set; } = string.Empty;

    public MetricaComparativaDto Ingresos { get; set; } = new();
    public MetricaComparativaDto Reservas { get; set; } = new();

    // Cambiamos a la nueva métrica
    public TendenciaReservaDto DiaMasFuerte { get; set; } = new();
}

public class MetricaComparativaDto
{
    public decimal ValorActual { get; set; }
    public decimal ValorAnterior { get; set; }
    public decimal PorcentajeCrecimiento { get; set; }
    public bool EsCrecimientoPositivo => PorcentajeCrecimiento >= 0;
}

public class TendenciaReservaDto
{
    public string Dia { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}