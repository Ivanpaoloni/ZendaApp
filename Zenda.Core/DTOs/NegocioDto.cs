namespace Zenda.Core.DTOs;

public class NegocioReadDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class NegocioCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    // El Slug se puede generar automáticamente desde el nombre (ej: "Zenda Barber" -> "zenda-barber")
}
public class NegocioUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }

    // TODO: A futuro agregaremos esto
    // public string? CategoriaId { get; set; } 
}