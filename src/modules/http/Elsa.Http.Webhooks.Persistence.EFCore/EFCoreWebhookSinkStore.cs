using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Persistence.Contracts;
using Elsa.Http.Webhooks.Persistence.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

[UsedImplicitly]
public class EFCoreWebhookSinkStore(WebhooksDbContext dbContext) : EFCoreWebhookSinkStore<WebhooksDbContext>(dbContext)
{
}

public class EFCoreWebhookSinkStore<TDbContext>(TDbContext dbContext) : IWebhookSinkStore where TDbContext : DbContext
{
    private DbSet<WebhookSinkRecord> Sinks => dbContext.Set<WebhookSinkRecord>();

    public async Task<PersistedWebhookSink> CreateAsync(PersistedWebhookSink sink, CancellationToken cancellationToken = default)
    {
        var record = ToRecord(sink);
        await Sinks.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToModel(record);
    }

    public async Task<PersistedWebhookSink?> FindAsync(string id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var record = await Sinks.FirstOrDefaultAsync(x => x.Id == id && (includeDeleted || !x.IsDeleted), cancellationToken);
        return record == null ? null : ToModel(record);
    }

    public async Task<IEnumerable<PersistedWebhookSink>> ListAsync(WebhookSinkFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new WebhookSinkFilter();

        IQueryable<WebhookSinkRecord> query = Sinks;

        if (!filter.IncludeDeleted)
            query = query.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Id))
            query = query.Where(x => x.Id == filter.Id);

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(x => x.Name == filter.Name);

        var items = await query.ToListAsync(cancellationToken);

        return items.Select(ToModel);
    }

    public async Task<PersistedWebhookSink?> UpdateAsync(PersistedWebhookSink sink, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var existing = await Sinks.FirstOrDefaultAsync(x => x.Id == sink.Id, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToModel(existing);
    }

    public async Task<bool> SoftDeleteAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var sink = await Sinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sink == null)
            return false;

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(sink.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while deleting sink.");

        sink.IsDeleted = true;
        sink.UpdatedAt = DateTimeOffset.UtcNow;
        sink.Version = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(string id, string? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var sink = await Sinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sink == null)
            return false;

        if (!string.IsNullOrWhiteSpace(expectedVersion) && !string.Equals(sink.Version, expectedVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency conflict while restoring sink.");

        sink.IsDeleted = false;
        sink.UpdatedAt = DateTimeOffset.UtcNow;
        sink.Version = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WebhookSinkRecord ToRecord(PersistedWebhookSink model)
    {
        return new WebhookSinkRecord
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

    private static PersistedWebhookSink ToModel(WebhookSinkRecord entity)
    {
        return new PersistedWebhookSink
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Url = new Uri(entity.Url),
            IsEnabled = entity.IsEnabled,
            IsDeleted = entity.IsDeleted,
            Headers = entity.Headers,
            Filters = entity.Filters,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
