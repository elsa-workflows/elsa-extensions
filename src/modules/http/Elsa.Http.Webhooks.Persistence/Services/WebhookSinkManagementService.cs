using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Persistence.Contracts;

namespace Elsa.Http.Webhooks.Persistence.Services;

public class WebhookSinkManagementService(IWebhookSinkStore store, IGenerateWebhookSinkId idGenerator) : IWebhookSinkManagementService
{
    public async Task<PersistedWebhookSink> CreateAsync(PersistedWebhookSink sink, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sink.Id))
            sink.Id = idGenerator.Generate();

        var duplicate = await store.FindAsync(sink.Id, includeDeleted: true, cancellationToken);

        if (duplicate != null)
            throw new InvalidOperationException($"A sink with id '{sink.Id}' already exists.");

        return await store.CreateAsync(sink, cancellationToken);
    }

    public Task<PersistedWebhookSink?> FindAsync(string id, bool includeDeleted = false, CancellationToken cancellationToken = default) =>
        store.FindAsync(id, includeDeleted, cancellationToken);

    public Task<IEnumerable<PersistedWebhookSink>> ListAsync(WebhookSinkFilter? filter = null, CancellationToken cancellationToken = default) =>
        store.ListAsync(filter, cancellationToken);

    public Task<PersistedWebhookSink?> UpdateAsync(PersistedWebhookSink sink, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
        store.UpdateAsync(sink, expectedVersion, cancellationToken);

    public Task<bool> DeleteAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
        store.SoftDeleteAsync(id, expectedVersion, cancellationToken);

    public Task<bool> RestoreAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
        store.RestoreAsync(id, expectedVersion, cancellationToken);
}
