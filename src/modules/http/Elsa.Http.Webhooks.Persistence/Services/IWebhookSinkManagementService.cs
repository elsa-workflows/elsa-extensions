using Elsa.Http.Webhooks.Abstractions.Contracts;

namespace Elsa.Http.Webhooks.Persistence.Services;

public interface IWebhookSinkManagementService
{
    Task<PersistedWebhookSink> CreateAsync(PersistedWebhookSink sink, CancellationToken cancellationToken = default);
    Task<PersistedWebhookSink?> FindAsync(string id, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<PersistedWebhookSink>> ListAsync(WebhookSinkFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PersistedWebhookSink?> UpdateAsync(PersistedWebhookSink sink, string? expectedVersion = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default);
}
