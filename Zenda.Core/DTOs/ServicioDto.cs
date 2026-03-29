using System.ComponentModel.DataAnnotations;

public class ServicioReadDto
{
    public Guid Id { get; set; }
    public Guid CategoriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
}

public class ServicioCreateDto
{
    [Required]
    public Guid CategoriaId { get; set; } // ¿A qué categoría pertenece?

    [Required(ErrorMessage = "El nombre del servicio es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Range(5, 480, ErrorMessage = "La duración debe ser entre 5 y 480 minutos.")]
    public int DuracionMinutos { get; set; } = 30;

    [Required]
    [Range(0, 9999999, ErrorMessage = "El precio no puede ser negativo.")]
    public decimal Precio { get; set; }
}