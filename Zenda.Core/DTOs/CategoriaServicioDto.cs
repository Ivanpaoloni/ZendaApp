using System.ComponentModel.DataAnnotations;

public class CategoriaServicioReadDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Icono { get; set; }
    // Acá viene la magia: la categoría ya trae sus servicios
    public List<ServicioReadDto> Servicios { get; set; } = new();
}

public class CategoriaServicioCreateDto
{
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;
}