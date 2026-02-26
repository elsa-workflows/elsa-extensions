using Elsa.Abstractions;
using Elsa.Http.Webhooks.Api.Contracts;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Get;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<Request, WebhookSinkModel>
{
    public override void Configure()
    {
        Get("/webhook-sinks/{id}");
        ConfigurePermissions("webhooks:read");
    }

    public override async Task<WebhookSinkModel> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var sink = await managementService.FindAsync(request.Id, request.IncludeDeleted, cancellationToken);

        if (sink == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return null!;
        }

        return sink.ToModel();
    }
}
