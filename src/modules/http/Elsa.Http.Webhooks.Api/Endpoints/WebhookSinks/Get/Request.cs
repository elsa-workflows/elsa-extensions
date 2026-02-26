namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Get;

public class Request
{
    public string Id { get; set; } = default!;
    public bool IncludeDeleted { get; set; }
}
