using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.MongoDb.Common;
using Elsa.Persistence.MongoDb.Modules.Identity;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Tenant-safe Mongo role lookups and upserts for elsa-core#8615.
/// <see cref="MongoRoleStore"/> filters through <see cref="RoleFilter.Apply"/> and
/// upserts through <see cref="MongoDbStore{TDocument}"/>'s tenant-owned filter.
/// </summary>
public sealed class MongoRoleStoreTests : IClassFixture<MongoRoleStoreTests.MongoFixture>
{
    private readonly MongoRoleStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public MongoRoleStoreTests(MongoFixture fixture)
    {
        var client = new MongoClient(fixture.Container.GetConnectionString());
        var database = client.GetDatabase($"elsa-role-store-{Guid.NewGuid():N}");
        _store = new MongoRoleStore(new MongoDbStore<Role>(database.GetCollection<Role>("roles"), _tenantAccessor));
    }

    [Fact]
    public async Task LegacyNameDerivedId_StillResolvesByIdAndIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("admin", "admin", "perm-a"));

        var byId = await _store.FindAsync(new RoleFilter { Id = "admin" });
        var byIds = (await _store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).ToList();

        Assert.NotNull(byId);
        Assert.Equal("admin", byId.Id);
        Assert.Equal("admin", byId.Name);
        Assert.Equal(["admin"], byIds.Select(x => x.Id));
    }

    [Fact]
    public async Task GeneratedId_ResolvesViaIds()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("role-generated-1", "admin", "perm-a"));

        var byIds = (await _store.FindManyAsync(new RoleFilter { Ids = ["role-generated-1"] })).ToList();
        var byId = await _store.FindAsync(new RoleFilter { Id = "role-generated-1" });
        var byLegacyNameId = await _store.FindAsync(new RoleFilter { Id = "admin" });

        Assert.Equal(["role-generated-1"], byIds.Select(x => x.Id));
        Assert.NotNull(byId);
        Assert.Equal("admin", byId.Name);
        Assert.Null(byLegacyNameId);
    }

    [Fact]
    public async Task TwoTenants_CanHoldSameNamedRole()
    {
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "operators", "perm-a"));

        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            await _store.SaveAsync(Role("role-b", "operators", "perm-b"));

        Role? tenantARole;
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            tenantARole = await _store.FindAsync(new RoleFilter { Id = "role-a" });

        Role? tenantBRole;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            tenantBRole = await _store.FindAsync(new RoleFilter { Id = "role-b" });

        Assert.NotNull(tenantARole);
        Assert.NotNull(tenantBRole);
        Assert.Equal("operators", tenantARole.Name);
        Assert.Equal("operators", tenantBRole.Name);
        Assert.Equal("tenant-a", tenantARole.TenantId);
        Assert.Equal("tenant-b", tenantBRole.TenantId);
    }

    [Fact]
    public async Task RoleInTenantA_IsInvisibleToTenantB()
    {
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("role-a", "operators", "perm-a"));

        using var tenantB = _tenantAccessor.PushContext(TenantB());
        var byId = await _store.FindAsync(new RoleFilter { Id = "role-a" });
        var byIds = await _store.FindManyAsync(new RoleFilter { Ids = ["role-a"] });
        var listed = await _store.FindManyAsync(new RoleFilter());

        Assert.Null(byId);
        Assert.Empty(byIds);
        Assert.Empty(listed);
    }

    [Fact]
    public async Task Upsert_CannotHijackAnotherTenantsRow()
    {
        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("admin", "admin", "perm-a"));

        using (var tenantA = _tenantAccessor.PushContext(TenantA()))
            await _store.SaveAsync(Role("admin", "admin", "perm-a-updated"));

        Exception? exception;
        using (var tenantB = _tenantAccessor.PushContext(TenantB()))
            exception = await Record.ExceptionAsync(() => _store.SaveAsync(Role("admin", "admin", "perm-b")));

        Assert.NotNull(exception);
        Assert.True(IsDuplicateKey(exception), exception.ToString());

        using var tenantARead = _tenantAccessor.PushContext(TenantA());
        var stored = await _store.FindAsync(new RoleFilter { Id = "admin" });

        Assert.NotNull(stored);
        Assert.Equal("tenant-a", stored.TenantId);
        Assert.Equal(["perm-a-updated"], stored.Permissions);
        Assert.Equal("admin", stored.Name);
    }

    private static bool IsDuplicateKey(Exception exception) =>
        exception switch
        {
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey || write.WriteError.Code == 11000,
            MongoCommandException command => command.Code == 11000,
            MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey || error.Code == 11000),
            _ => exception.InnerException is { } inner && IsDuplicateKey(inner)
        };

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static Role Role(string id, string name, string permission) => new()
    {
        Id = id,
        Name = name,
        Permissions = [permission]
    };

    public sealed class MongoFixture : IAsyncLifetime
    {
        public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();

        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
        public Tenant? Tenant { get; private set; }

        public IDisposable PushContext(Tenant? tenant)
        {
            var previousTenant = Tenant;
            Tenant = tenant;
            return new Restore(() => Tenant = previousTenant);
        }

        private sealed class Restore(Action restore) : IDisposable
        {
            public void Dispose() => restore();
        }
    }
}
