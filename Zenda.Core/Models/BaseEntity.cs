namespace Zenda.Core.Models;

/// <summary>
/// Clase base para estandarizar la identidad, el borrado lógico y la trazabilidad (auditoría).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Prevención de pérdida de datos (Soft Delete)
    public bool IsDeleted { get; set; } = false;

    // Trazabilidad temporal (Siempre en UTC para evitar bugs de zonas horarias)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Trazabilidad de autoría (Quién hizo qué)
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}