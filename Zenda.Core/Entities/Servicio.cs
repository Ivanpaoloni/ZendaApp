using Zenda.Core.Entities;

public class Servicio
{
    public Guid Id { get; set; }
    public Guid NegocioId { get; set; }
    public Guid CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; } = true;

    // Relaciones
    public CategoriaServicio Categoria { get; set; } = null!;
    public ICollection<Prestador> Prestadores { get; set; } = new List<Prestador>();
}