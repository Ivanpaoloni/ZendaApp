using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Negocio : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // Para su link público

    // Lista de sus sucursales
    public List<Sede> Sedes { get; set; } = new();
}