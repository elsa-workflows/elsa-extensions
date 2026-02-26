using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Persistence.Contracts;

namespace Elsa.Http.Webhooks.Persistence.Services;

public class MemoryWebhookSinkStore : IWebhookSinkStore
{
    private readonly Dictionary<string, PersistedWebhookSink> _sinks = new(StringComparer.OrdinalIgnoreCase);

    public Task<PersistedWebhookSink> CreateAsync(PersistedWebhookSink sink, CancellationToken cancellationToken = default)
    {
        if (_sinks.ContainsKey(sink.Id))
            throw new InvalidOperationException($"A sink with id '{sink.Id}' already exists.");

        sink.Version = Guid.NewGuid().ToString("N");
        sink.CreatedAt = DateTimeOffset.UtcNow;
        sink.UpdatedAt = sink.CreatedAt;
        _sinks[sink.Id] = sink;
        return Task.FromResult(sink);
    }

    public Task<PersistedWebhookSink?> FindAsync(string id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        _sinks.TryGetValue(id, out var sink);

        if (sink == null)
            return Task.FromResult<PersistedWebhookSink?>(null);

        if (!includeDeleted && sink.IsDeleted)
            return Task.FromResult<PersistedWebhookSink?>(null);

        return Task.FromResult<PersistedWebhookSink?>(sink);
    }

    public Task<IEnumerable<PersistedWebhookSink>> ListAsync(WebhookSinkFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _sinks.Values.AsEnumerable();
        filter ??= new WebhookSinkFilter();

        if (!filter.IncludeDeleted)
            query = query.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Id))
            query = query.Where(x => string.Equals(x.Id, filter.Id, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(x => string.Equals(x.Name, filter.Name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(query);
    }

    public Task<PersistedWebhookSink?> UpdateAsync(PersistedWebhookSink sink, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        if (!_sinks.TryGetValue(sink.Id, out var existing))
            return Task.FromResult<PersistedWebhookSink?>(null);

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(existing.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while updating sink.");

        sink.CreatedAt = existing.CreatedAt;
        sink.UpdatedAt = DateTimeOffset.UtcNow;
        sink.Version = Guid.NewGuid().ToString("N");
        _sinks[sink.Id] = sink;
        return Task.FromResult<PersistedWebhookSink?>(sink);
    }

    public Task<bool> SoftDeleteAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        if (!_sinks.TryGetValue(id, out var sink))
            return Task.FromResult(false);

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(sink.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while deleting sink.");

        sink.IsDeleted = true;
        sink.UpdatedAt = DateTimeOffset.UtcNow;
        sink.Version = Guid.NewGuid().ToString("N");
        return Task.FromResult(true);
    }

    public Task<bool> RestoreAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        if (!_sinks.TryGetValue(id, out var sink))
            return Task.FromResult(false);

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(sink.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while restoring sink.");

        sink.IsDeleted = false;
        sink.UpdatedAt = DateTimeOffset.UtcNow;
        sink.Version = Guid.NewGuid().ToString("N");
        return Task.FromResult(true);
    }
}
