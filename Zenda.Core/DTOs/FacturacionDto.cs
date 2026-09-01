namespace Zenda.Core.DTOs;

public class FacturacionDto
{
    public Guid PlanActualId { get; set; }
    public string PlanActualNombre { get; set; } = string.Empty;
    public decimal PlanActualPrecio { get; set; }
    public string Estado { get; set; } = string.Empty; // Activa, Vencido, PorVencer
    public DateTime FechaVencimiento { get; set; }
    public bool ProximoAVencer { get; set; } // 🎯 Indica si debe ofrecerse el botón de renovación

    // Límites y métricas de consumo
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