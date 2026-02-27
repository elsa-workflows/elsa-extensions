using Elsa.Http.Webhooks.Api.Contracts;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Update;

public class Request : WebhookSinkInputModel
{
    public new string Id { get; set; } = null!;
}
