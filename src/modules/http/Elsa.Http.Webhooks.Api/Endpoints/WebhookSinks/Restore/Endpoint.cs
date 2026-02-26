using Elsa.Abstractions;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Restore;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Post("/webhook-sinks/{id}/restore");
        ConfigurePermissions("webhooks:write");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        try
        {
            var restored = await managementService.RestoreAsync(request.Id, request.ExpectedVersion, cancellationToken);

            if (!restored)
                await Send.NotFoundAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            await Send.ResultAsync(new ConflictObjectResult(new { Error = ex.Message }));
        }
    }
}
