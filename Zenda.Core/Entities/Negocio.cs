using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Negocio : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // Para su link público
    public string LogoUrl { get; set; } = string.Empty; // Para su link público
    public int AnticipacionMinimaHoras { get; set; } = 2;
    public int VentanaReservaDias { get; set; } = 30;
    // Lista de sus sucursales
    public List<Sede> Sedes { get; set; } = new();
}