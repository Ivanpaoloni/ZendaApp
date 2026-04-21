namespace Zenda.Core.DTOs;

public class GenerarLinkDto
{
    public Guid PlanId { get; set; }
    public string NombrePlan { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}

public class GenerarLinkResponseDto
{
    public string UrlCheckout { get; set; } = string.Empty;
}