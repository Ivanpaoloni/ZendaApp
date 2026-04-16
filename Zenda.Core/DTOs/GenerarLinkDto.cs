namespace Zenda.Core.DTOs;

public record GenerarLinkDto
{
    public Guid PlanId { get; init; }
    public string NombrePlan { get; init; } = string.Empty;
    public decimal Precio { get; init; }
}