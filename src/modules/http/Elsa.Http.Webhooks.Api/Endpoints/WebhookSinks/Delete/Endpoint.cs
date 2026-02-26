using Elsa.Abstractions;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Delete;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/webhook-sinks/{id}");
        ConfigurePermissions("webhooks:delete");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await managementService.DeleteAsync(request.Id, request.ExpectedVersion, cancellationToken);

            if (!deleted)
                await Send.NotFoundAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await Send.ResultAsync(new ConflictObjectResult(new { Error = ex.Message }));
        }
    }
}
