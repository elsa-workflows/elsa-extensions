namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Delete;

public class Request
{
    public string Id { get; set; } = default!;
    public string? ExpectedVersion { get; set; }
}
