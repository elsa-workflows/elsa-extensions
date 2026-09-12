using Elsa.Common.Multitenancy;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.MongoDb.UnitTests;

public class MongoWorkflowInstanceStoreTests
{
    [Fact(DisplayName = "TryMarkInterruptedAsync updates only when Status is not Finished")]
    public async Task TryMarkInterruptedAsync_UpdatesNonTerminalInstance()
    {
        var (store, collection) = CreateStore(matchedCount: 1);

        var marked = await store.TryMarkInterruptedAsync("running-1");

        Assert.True(marked);
        await collection.Received(1).UpdateOneAsync(
            Arg.Is<FilterDefinition<WorkflowInstance>>(filter => FilterMentionsIdAndNonFinished(filter, "running-1")),
            Arg.Is<UpdateDefinition<WorkflowInstance>>(update => UpdateSetsInterruptedMarkers(update)),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TryMarkInterruptedAsync returns false when no non-terminal instance matches")]
    public async Task TryMarkInterruptedAsync_ReturnsFalseWhenMissingOrFinished()
    {
        var (store, _) = CreateStore(matchedCount: 0);

        var marked = await store.TryMarkInterruptedAsync("finished-1");

        Assert.False(marked);
    }

    private static (MongoWorkflowInstanceStore Store, IMongoCollection<WorkflowInstance> Collection) CreateStore(long matchedCount)
    {
        var collection = Substitute.For<IMongoCollection<WorkflowInstance>>();
        var updateResult = Substitute.For<UpdateResult>();
        updateResult.IsAcknowledged.Returns(true);
        updateResult.MatchedCount.Returns(matchedCount);
        updateResult.ModifiedCount.Returns(matchedCount);

        collection.UpdateOneAsync(
                Arg.Any<FilterDefinition<WorkflowInstance>>(),
                Arg.Any<UpdateDefinition<WorkflowInstance>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(updateResult);

        var mongoDbStore = new MongoDbStore<WorkflowInstance>(collection, Substitute.For<ITenantAccessor>());
        var store = new MongoWorkflowInstanceStore(mongoDbStore, Substitute.For<ILogger<MongoWorkflowInstanceStore>>());
        return (store, collection);
    }

    private static bool FilterMentionsIdAndNonFinished(FilterDefinition<WorkflowInstance> filter, string id)
    {
        var rendered = Render(filter);
        return rendered.Contains(id, StringComparison.Ordinal)
               && rendered.Contains("Status", StringComparison.Ordinal)
               && rendered.Contains("$ne", StringComparison.Ordinal)
               && rendered.Contains(((int)WorkflowStatus.Finished).ToString(), StringComparison.Ordinal);
    }

    private static bool UpdateSetsInterruptedMarkers(UpdateDefinition<WorkflowInstance> update)
    {
        var rendered = Render(update);
        return rendered.Contains("$set", StringComparison.Ordinal)
               && rendered.Contains("SubStatus", StringComparison.Ordinal)
               && rendered.Contains(((int)WorkflowSubStatus.Interrupted).ToString(), StringComparison.Ordinal)
               && rendered.Contains("IsExecuting", StringComparison.Ordinal);
    }

    private static string Render(FilterDefinition<WorkflowInstance> filter)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        var rendered = filter.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
        return rendered.ToJson();
    }

    private static string Render(UpdateDefinition<WorkflowInstance> update)
    {
        var serializer = BsonSerializer.LookupSerializer<WorkflowInstance>();
        var rendered = update.Render(new RenderArgs<WorkflowInstance>(serializer, BsonSerializer.SerializerRegistry));
        return rendered.ToJson();
    }
}
