using Elsa.Abstractions;
using Elsa.Http.Webhooks.Api.Contracts;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Create;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<WebhookSinkInputModel, WebhookSinkModel>
{
    public override void Configure()
    {
        Post("/webhook-sinks");
        ConfigurePermissions("webhooks:write");
    }

    public override async Task<WebhookSinkModel> ExecuteAsync(WebhookSinkInputModel request, CancellationToken cancellationToken)
    {
        var sink = await managementService.CreateAsync(request.ToEntity(), cancellationToken);
        return sink.ToModel();
    }
}
