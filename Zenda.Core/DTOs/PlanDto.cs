namespace Zenda.Core.DTOs;

public class PlanVistaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PrecioTexto { get; set; } = string.Empty;
    public decimal PrecioMensual { get; set; }
    public int MaxSedes { get; set; }
    public int MaxProfesionales { get; set; }
    public bool HabilitaRecordatorios { get; set; }
}