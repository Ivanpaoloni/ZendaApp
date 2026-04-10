using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class Rubro : BaseEntity
{
    public string Nombre { get; set; } = string.Empty; // Ej: "Barbería", "Centro de Estética"
    public string Codigo { get; set; } = string.Empty; // Ej: "BARBERIA", "ESTETICA" (útil para validaciones rápidas en código)
    public bool Activo { get; set; } = true;

    // Un rubro tiene muchos negocios
    public ICollection<Negocio> Negocios { get; set; } = new List<Negocio>();
}