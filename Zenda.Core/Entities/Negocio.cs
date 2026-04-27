using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Negocio : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // Para su link público
    public string LogoUrl { get; set; } = string.Empty; // Para su link público
    public int AnticipacionMinimaHoras { get; set; } = 0;
    public int VentanaReservaDias { get; set; } = 30;
    public int IntervaloTurnosMinutos { get; set; } = 30; // Por defecto cada 30 min

    public Guid RubroId { get; set; }
    public Rubro Rubro { get; set; } = null!;

    public List<Sede> Sedes { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public string? NotasAdmin { get; set; }

    public Guid PlanSuscripcionId { get; set; }
    public PlanSuscripcion PlanSuscripcion { get; set; } = null!;
}