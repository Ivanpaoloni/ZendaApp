public class CategoriaServicio
{
    public Guid Id { get; set; }
    public Guid NegocioId { get; set; } // El dueño de la categoría
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; } // Para poner un dibujito en la web (opcional)
    public bool Activo { get; set; } = true;
    // Relaciones
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}