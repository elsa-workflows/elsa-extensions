using Elsa.Common.Multitenancy;
using Elsa.Common.Entities;
using Elsa.KeyValues.Entities;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Runtime;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.MongoDb.UnitTests;

public class MongoKeyValueStoreTests
{
    private readonly MongoDbStore<SerializedKeyValuePair> _mongoDbStore;

    public MongoKeyValueStoreTests()
    {
        var mongoCollectionMock = Substitute.For<IMongoCollection<SerializedKeyValuePair>>();
        var tenantAccessorMock = Substitute.For<ITenantAccessor>();
        mongoCollectionMock.FindOneAndReplaceAsync(
                Arg.Any<FilterDefinition<SerializedKeyValuePair>>(),
                Arg.Any<SerializedKeyValuePair>(),
                Arg.Any<FindOneAndReplaceOptions<SerializedKeyValuePair>>()
            )
            .Returns(new SerializedKeyValuePair());
        _mongoDbStore = new MongoDbStore<SerializedKeyValuePair>(mongoCollectionMock, tenantAccessorMock);
    }

    [Fact(DisplayName = "When saving a SerializedKeyValuePair document, don't throw an exception of missing ID property")]
    public async Task SaveAsync_WithSerializedKeyValuePairDocument_DoesNotThrowException()
    {
        var mongoKeyValueStore = new MongoKeyValueStore(_mongoDbStore);
        var keyValuePair = new SerializedKeyValuePair();

        var exception = await Record.ExceptionAsync(
            async () => await mongoKeyValueStore.SaveAsync(keyValuePair, default));

        Assert.Null(exception);
    }
}

public class MongoDbStoreTenantTests
{
    [Fact(DisplayName = "When saving an agnostic entity, preserve the agnostic tenant marker")]
    public async Task SaveAsync_WithAgnosticTenant_PreservesAgnosticMarker()
    {
        var mongoCollectionMock = Substitute.For<IMongoCollection<TestEntity>>();
        mongoCollectionMock.FindOneAndReplaceAsync(
                Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<TestEntity>(),
                Arg.Any<FindOneAndReplaceOptions<TestEntity>>()
            )
            .Returns(callInfo => callInfo.Arg<TestEntity>());

        var tenantAccessorMock = Substitute.For<ITenantAccessor>();
        tenantAccessorMock.Tenant.Returns(new Tenant { Id = "tenant-a", Name = "tenant-a" });
        var store = new MongoDbStore<TestEntity>(mongoCollectionMock, tenantAccessorMock);
        var entity = new TestEntity { Id = "1", TenantId = Tenant.AgnosticTenantId };

        await store.SaveAsync(entity);

        Assert.Equal(Tenant.AgnosticTenantId, entity.TenantId);
    }

    [Fact(DisplayName = "When saving an unscoped entity under a tenant, stamp ambient tenant")]
    public async Task SaveAsync_WithNullTenant_StampsAmbientTenant()
    {
        var mongoCollectionMock = Substitute.For<IMongoCollection<TestEntity>>();
        mongoCollectionMock.FindOneAndReplaceAsync(
                Arg.Any<FilterDefinition<TestEntity>>(),
                Arg.Any<TestEntity>(),
                Arg.Any<FindOneAndReplaceOptions<TestEntity>>()
            )
            .Returns(callInfo => callInfo.Arg<TestEntity>());

        var tenantAccessorMock = Substitute.For<ITenantAccessor>();
        tenantAccessorMock.Tenant.Returns(new Tenant { Id = "tenant-a", Name = "tenant-a" });
        var store = new MongoDbStore<TestEntity>(mongoCollectionMock, tenantAccessorMock);
        var entity = new TestEntity { Id = "1", TenantId = null };

        await store.SaveAsync(entity);

        Assert.Equal("tenant-a", entity.TenantId);
    }

    [Fact(DisplayName = "Tenant filter includes tenant-specific and agnostic records")]
    public void ApplyTenantFilter_WithConcreteTenant_IncludesAgnostic()
    {
        var documents = new[]
        {
            new TestEntity { Id = "specific", TenantId = "tenant-a" },
            new TestEntity { Id = "agnostic", TenantId = Tenant.AgnosticTenantId },
            new TestEntity { Id = "other", TenantId = "tenant-b" },
            new TestEntity { Id = "null", TenantId = null }
        }.AsQueryable();

        var filtered = MongoDbStore<TestEntity>.ApplyTenantFilter(documents, "tenant-a");
        var ids = filtered.Select(x => x.Id).ToList();

        Assert.Contains("specific", ids);
        Assert.Contains("agnostic", ids);
        Assert.DoesNotContain("other", ids);
        Assert.DoesNotContain("null", ids);
    }

    [Fact(DisplayName = "Tenant filter includes null and agnostic when ambient tenant is agnostic")]
    public void ApplyTenantFilter_WithAgnosticTenant_IncludesNullAndAgnostic()
    {
        var documents = new[]
        {
            new TestEntity { Id = "specific", TenantId = "tenant-a" },
            new TestEntity { Id = "agnostic", TenantId = Tenant.AgnosticTenantId },
            new TestEntity { Id = "null", TenantId = null }
        }.AsQueryable();

        var filtered = MongoDbStore<TestEntity>.ApplyTenantFilter(documents, null);
        var ids = filtered.Select(x => x.Id).ToList();

        Assert.Contains("agnostic", ids);
        Assert.Contains("null", ids);
        Assert.DoesNotContain("specific", ids);
    }

    public class TestEntity : Entity;
}