using Zenda.Core.Models;

namespace Zenda.Core.Entities;

public class BloqueoAgenda : BaseEntity
{
    public Guid PrestadorId { get; set; }
    public Guid SedeId { get; set; }

    public DateTime InicioUtc { get; set; } // Ejemplo: 2026-04-15 14:00
    public DateTime FinUtc { get; set; }    // Ejemplo: 2026-04-15 18:00

    public string? Motivo { get; set; } // "Trámite personal", "Médico", etc.

    // Relaciones
    public Prestador Prestador { get; set; } = null!;
    public Sede Sede { get; set; } = null!;
}