namespace Zenda.Core.DTOs;

public class FacturacionDto
{
    public string PlanActualNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // Activa, Vencida, etc.
    public DateTime FechaVencimiento { get; set; }

    // Para las barras de progreso
    public int SedesUsadas { get; set; }
    public int SedesMaximas { get; set; }
    public int ProfesionalesUsados { get; set; }
    public int ProfesionalesMaximos { get; set; }

    public List<HistorialPagoDto> Pagos { get; set; } = new();
}

public class HistorialPagoDto
{
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string PlanNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = "Aprobado";
    public string TransaccionId { get; set; } = string.Empty;
}