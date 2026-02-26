using Elsa.Abstractions;
using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Api.Contracts;
using Elsa.Http.Webhooks.Persistence.Services;
using JetBrains.Annotations;

namespace Elsa.Http.Webhooks.Api.Endpoints.WebhookSinks.List;

[UsedImplicitly]
public class Endpoint(IWebhookSinkManagementService managementService) : ElsaEndpoint<ListWebhookSinksRequest, WebhookSinkListResponse>
{
    public override void Configure()
    {
        Get("/webhook-sinks");
        ConfigurePermissions("webhooks:read");
    }

    public override async Task<WebhookSinkListResponse> ExecuteAsync(ListWebhookSinksRequest request, CancellationToken cancellationToken)
    {
        var filter = new WebhookSinkFilter
        {
            Name = request.Name,
            IncludeDeleted = request.IncludeDeleted
        };

        var items = await managementService.ListAsync(filter, cancellationToken);

        return new WebhookSinkListResponse
        {
            Items = items.Select(x => x.ToModel()).ToList()
        };
    }
}
