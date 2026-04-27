namespace Zenda.Core.DTOs.Admin;

public class NegocioAdminListDto
{
    public Guid SuscripcionId { get; set; }
    public Guid NegocioId { get; set; }
    public string NombreNegocio { get; set; } = string.Empty;
    public string PlanNombre { get; set; } = string.Empty;

    public Guid PlanSuscripcionId { get; set; }

    public bool IsActive { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoMensual { get; set; }
}

public class AdminUpdateNegocioDto
{
    public bool IsActive { get; set; }
    public Guid PlanSuscripcionId { get; set; }
    public decimal? PrecioMensualPersonalizado { get; set; }
    public DateTime FechaVencimiento { get; set; }
}
public class PlanAdminDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}