using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// An EF Core implementation of <see cref="IActivityExecutionStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreActivityExecutionStore(
    EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord> store,
    ISafeSerializer safeSerializer,
    IPayloadSerializer payloadSerializer,
    ICompressionCodecResolver compressionCodecResolver,
    IOptions<ManagementOptions> options) : IActivityExecutionStore
{
    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default) => await store.SaveAsync(record, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await store.SaveManyAsync(records, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task AddManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default) => await store.AddManyAsync(records, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Persistence.EFCore.Modules.Runtime.EFCoreActivityExecutionStore.DeserializeActivityState(RuntimeElsaDbContext, ActivityExecutionRecord, CancellationToken)")]
    public async Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Persistence.EFCore.Modules.Runtime.EFCoreActivityExecutionStore.DeserializeActivityState(RuntimeElsaDbContext, ActivityExecutionRecord, CancellationToken)")]
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(query => Filter(query, filter), ActivityExecutionRecordSummary.FromRecordExpression(), cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(query => Filter(query, filter), ActivityExecutionRecordSummary.FromRecordExpression(), cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    private async ValueTask OnSaveAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        entity = entity.SanitizeLogMessage();
        var compressionAlgorithm = options.Value.CompressionAlgorithm ?? nameof(None);
        var serializedActivityState = entity.ActivityState?.Count > 0 ? safeSerializer.Serialize(entity.ActivityState) : null;
        var compressedSerializedActivityState = serializedActivityState != null ? await compressionCodecResolver.Resolve(compressionAlgorithm).CompressAsync(serializedActivityState, cancellationToken) : null;
        var serializedProperties = entity.Properties != null ? payloadSerializer.Serialize(entity.Properties) : null;
        var serializedMetadata = entity.Metadata != null ? payloadSerializer.Serialize(entity.Metadata) : null;

        dbContext.Entry(entity).Property("SerializedActivityState").CurrentValue = compressedSerializedActivityState;
        dbContext.Entry(entity).Property("SerializedActivityStateCompressionAlgorithm").CurrentValue = compressionAlgorithm;
        dbContext.Entry(entity).Property("SerializedOutputs").CurrentValue = entity.Outputs?.Any() == true ? safeSerializer.Serialize(entity.Outputs) : null;
        dbContext.Entry(entity).Property("SerializedProperties").CurrentValue = serializedProperties;
        dbContext.Entry(entity).Property("SerializedMetadata").CurrentValue = serializedMetadata;
        dbContext.Entry(entity).Property("SerializedException").CurrentValue = entity.Exception != null ? payloadSerializer.Serialize(entity.Exception) : null;
        dbContext.Entry(entity).Property("SerializedPayload").CurrentValue = entity.Payload?.Any() == true ? payloadSerializer.Serialize(entity.Payload) : null;
    }

    [RequiresUnreferencedCode("Calls Elsa.Persistence.EFCore.Modules.Runtime.EFCoreActivityExecutionStore.DeserializeActivityState(RuntimeElsaDbContext, ActivityExecutionRecord, CancellationToken)")]
    private async ValueTask OnLoadAsync(RuntimeElsaDbContext dbContext, ActivityExecutionRecord? entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            return;

        entity.ActivityState = await DeserializeActivityState(dbContext, entity, cancellationToken);
        entity.Outputs = Deserialize<IDictionary<string, object?>>(dbContext, entity, "SerializedOutputs");
        entity.Properties = DeserializePayload<IDictionary<string, object>?>(dbContext, entity, "SerializedProperties");
        entity.Metadata = DeserializePayload<IDictionary<string, object>?>(dbContext, entity, "SerializedMetadata");
        entity.Exception = DeserializePayload<ExceptionState>(dbContext, entity, "SerializedException");
        entity.Payload = DeserializePayload<IDictionary<string, object>>(dbContext, entity, "SerializedPayload");
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private async Task<IDictionary<string, object?>?> DeserializeActivityState(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, CancellationToken cancellationToken)
    {
        var json = dbContext.Entry(entity).Property<string>("SerializedActivityState").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var compressionAlgorithm = (string?)dbContext.Entry(entity).Property("SerializedActivityStateCompressionAlgorithm").CurrentValue ?? nameof(None);
            var compressionStrategy = compressionCodecResolver.Resolve(compressionAlgorithm);
            json = await compressionStrategy.DecompressAsync(json, cancellationToken);
            var dictionary = JsonSerializer.Deserialize<IDictionary<string, object?>>(json);
            return dictionary?.ToDictionary(x => x.Key, x => x.Value);
        }

        return null;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private T Deserialize<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var value = !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<T>(json) : default;
        return value!;
    }

    private T DeserializePayload<T>(RuntimeElsaDbContext dbContext, ActivityExecutionRecord entity, string propertyName)
    {
        var json = dbContext.Entry(entity).Property<string>(propertyName).CurrentValue;
        var payload = !string.IsNullOrEmpty(json) ? payloadSerializer.Deserialize<T>(json) : default;
        return payload!;
    }

    private static IQueryable<ActivityExecutionRecord> Filter(IQueryable<ActivityExecutionRecord> queryable, ActivityExecutionRecordFilter filter) => filter.Apply(queryable);
}