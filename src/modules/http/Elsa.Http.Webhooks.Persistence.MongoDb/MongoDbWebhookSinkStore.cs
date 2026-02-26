using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Persistence.Contracts;
using Elsa.Http.Webhooks.Persistence.MongoDb.Entities;
using Elsa.Http.Webhooks.Persistence.MongoDb.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Elsa.Http.Webhooks.Persistence.MongoDb;

public class MongoDbWebhookSinkStore : IWebhookSinkStore
{
    private readonly IMongoCollection<WebhookSinkDocument> _collection;

    public MongoDbWebhookSinkStore(IMongoDatabase database, IOptions<MongoDbWebhookPersistenceOptions> options)
    {
        _collection = database.GetCollection<WebhookSinkDocument>(options.Value.CollectionName);
    }

    public async Task<PersistedWebhookSink> CreateAsync(PersistedWebhookSink sink, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(sink);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return ToModel(document);
    }

    public async Task<PersistedWebhookSink?> FindAsync(string id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var filter = Builders<WebhookSinkDocument>.Filter.Eq(x => x.Id, id);

        if (!includeDeleted)
            filter &= Builders<WebhookSinkDocument>.Filter.Eq(x => x.IsDeleted, false);

        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document == null ? null : ToModel(document);
    }

    public async Task<IEnumerable<PersistedWebhookSink>> ListAsync(WebhookSinkFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new WebhookSinkFilter();
        var filters = new List<FilterDefinition<WebhookSinkDocument>>();

        if (!filter.IncludeDeleted)
            filters.Add(Builders<WebhookSinkDocument>.Filter.Eq(x => x.IsDeleted, false));

        if (!string.IsNullOrWhiteSpace(filter.Id))
            filters.Add(Builders<WebhookSinkDocument>.Filter.Eq(x => x.Id, filter.Id));

        if (!string.IsNullOrWhiteSpace(filter.Name))
            filters.Add(Builders<WebhookSinkDocument>.Filter.Eq(x => x.Name, filter.Name));

        var query = filters.Count > 0 ? Builders<WebhookSinkDocument>.Filter.And(filters) : Builders<WebhookSinkDocument>.Filter.Empty;
        var items = await _collection.Find(query).ToListAsync(cancellationToken);
        return items.Select(ToModel);
    }

    public async Task<PersistedWebhookSink?> UpdateAsync(PersistedWebhookSink sink, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var existing = await _collection.Find(x => x.Id == sink.Id).FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
            return null;

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(existing.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while updating sink.");

        existing.Name = sink.Name;
        existing.Description = sink.Description;
        existing.Url = sink.Url.ToString();
        existing.IsEnabled = sink.IsEnabled;
        existing.IsDeleted = sink.IsDeleted;
        existing.Headers = sink.Headers.ToDictionary(x => x.Key, x => x.Value);
        existing.Filters = sink.Filters.ToList();
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.Version = Guid.NewGuid().ToString("N");

        await _collection.ReplaceOneAsync(x => x.Id == existing.Id, existing, cancellationToken: cancellationToken);
        return ToModel(existing);
    }

    public async Task<bool> SoftDeleteAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var existing = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
            return false;

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(existing.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while deleting sink.");

        existing.IsDeleted = true;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.Version = Guid.NewGuid().ToString("N");
        await _collection.ReplaceOneAsync(x => x.Id == existing.Id, existing, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var existing = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
            return false;

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(existing.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while restoring sink.");

        existing.IsDeleted = false;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.Version = Guid.NewGuid().ToString("N");
        await _collection.ReplaceOneAsync(x => x.Id == existing.Id, existing, cancellationToken: cancellationToken);
        return true;
    }

    private static WebhookSinkDocument ToDocument(PersistedWebhookSink model)
    {
        return new WebhookSinkDocument
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            Url = model.Url.ToString(),
            IsEnabled = model.IsEnabled,
            IsDeleted = model.IsDeleted,
            Headers = model.Headers.ToDictionary(x => x.Key, x => x.Value),
            Filters = model.Filters.ToList(),
            Version = model.Version ?? Guid.NewGuid().ToString("N"),
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    private static PersistedWebhookSink ToModel(WebhookSinkDocument document)
    {
        return new PersistedWebhookSink
        {
            Id = document.Id,
            Name = document.Name,
            Description = document.Description,
            Url = new Uri(document.Url),
            IsEnabled = document.IsEnabled,
            IsDeleted = document.IsDeleted,
            Headers = document.Headers,
            Filters = document.Filters,
            Version = document.Version,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }
}
