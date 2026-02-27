namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Delete;

public class Request
{
    public string Id { get; set; } = null!;
    public string? ExpectedVersion { get; set; }
}
