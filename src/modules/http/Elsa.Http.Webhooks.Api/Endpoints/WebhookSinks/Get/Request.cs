namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Get;

public class Request
{
    public string Id { get; set; } = null!;
    public bool IncludeDeleted { get; set; }
}
