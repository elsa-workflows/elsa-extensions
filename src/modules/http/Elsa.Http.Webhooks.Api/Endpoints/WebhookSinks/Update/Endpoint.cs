using Elsa.Abstractions;
using Elsa.Http.Webhooks.Api.Contracts;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.Update;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<Request, WebhookSinkModel>
{
    public override void Configure()
    {
        Post("/webhook-sinks/{id}");
        ConfigurePermissions("webhooks:write");
    }

    public override async Task<WebhookSinkModel> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        try
        {
            var sink = await managementService.UpdateAsync(request.ToEntity(request.Id), request.ExpectedVersion, cancellationToken);

            if (sink == null)
            {
                await Send.NotFoundAsync(cancellationToken);
                return null!;
            }

            return sink.ToModel();
        }
        catch (InvalidOperationException ex)
        {
            await Send.ResultAsync(new ConflictObjectResult(new { Error = ex.Message }));
            return null!;
        }
    }
}
