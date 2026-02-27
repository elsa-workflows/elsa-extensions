using Elsa.Http.Webhooks.Api.Contracts;
using Refit;

namespace Elsa.Studio.Http.Webhooks.Client;

public interface IWebhookSinksApi
{
    [Get("/webhook-sinks")]
    Task<WebhookSinkListResponse> ListAsync([Query] ListWebhookSinksRequest request, CancellationToken cancellationToken = default);

    [Get("/webhook-sinks/{id}")]
    Task<WebhookSinkModel> GetAsync(string id, [AliasAs("includeDeleted")] bool includeDeleted = false, CancellationToken cancellationToken = default);

    [Post("/webhook-sinks")]
    Task<WebhookSinkModel> CreateAsync(WebhookSinkInputModel request, CancellationToken cancellationToken = default);

    [Post("/webhook-sinks/{id}")]
    Task<WebhookSinkModel> UpdateAsync(string id, WebhookSinkInputModel request, CancellationToken cancellationToken = default);

    [Delete("/webhook-sinks/{id}")]
    Task DeleteAsync(string id, DeleteWebhookSinkRequest request, CancellationToken cancellationToken = default);

    [Post("/webhook-sinks/{id}/restore")]
    Task RestoreAsync(string id, RestoreWebhookSinkRequest request, CancellationToken cancellationToken = default);
}
