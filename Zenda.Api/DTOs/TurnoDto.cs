using System.ComponentModel.DataAnnotations;

namespace Zenda.Api.DTOs;

public class TurnoCreateDto
{
    public Guid PrestadorId { get; set; }

    [Required]
    public DateTime Inicio { get; set; } // El estándar es recibir ISO 8601

    [Required]
    [EmailAddress] // Validación automática de formato de mail
    public string ClienteEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string ClienteNombre { get; set; } = string.Empty;
}

public class TurnoReadDto
{
    public Guid Id { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public bool EstaConfirmado { get; set; }
}