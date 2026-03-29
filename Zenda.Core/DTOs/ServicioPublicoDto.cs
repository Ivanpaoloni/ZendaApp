namespace Zenda.Core.DTOs;

public class ServicioPublicoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }

    // Acá está la magia: aplanamos la relación
    public string CategoriaNombre { get; set; } = string.Empty;
}