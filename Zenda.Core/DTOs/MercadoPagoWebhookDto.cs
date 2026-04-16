namespace Zenda.Core.DTOs;

public record MercadoPagoWebhookDto
{
    public string? Action { get; init; }
    public string? Type { get; init; }
    public string? Date_created { get; init; }
    public WebhookData? Data { get; init; }
}

public record WebhookData
{
    public long? Id { get; init; }
}