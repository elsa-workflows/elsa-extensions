using Elsa.Http.Webhooks.Api.Contracts;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Restore;

public class Request : RestoreWebhookSinkRequest
{
    public string Id { get; set; } = default!;
}
