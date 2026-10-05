using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Modules.Identity.Stores;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-safe Dapper role lookups and upserts for elsa-core#8615.
/// Role ids are no longer derived from the name; <see cref="RoleFilter.Ids"/> must
/// match the Id column, and an upsert must not re-home another tenant's row.
/// </summary>
public sealed class DapperRoleStoreTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), Path.GetFileName($"elsa-dapper-roles-{Guid.NewGuid():N}.db"));
    private readonly DapperRoleStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public DapperRoleStoreTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
                          create table Roles (
                              Id text not null primary key,
                              Name text not null,
                              Permissions text not null,
                              TenantId text null
                          );
                          """);

        var store = new Store<RoleRecord>(connectionProvider, _tenantAccessor, "Roles");
        _store = new DapperRoleStore(store);
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
    public async Task GeneratedId_ResolvesViaIds_AndIdsDoNotMatchName()
    {
        using var tenantScope = _tenantAccessor.PushContext(TenantA());
        await _store.SaveAsync(Role("role-generated-1", "admin", "perm-a"));

        var byIds = (await _store.FindManyAsync(new RoleFilter { Ids = ["role-generated-1"] })).ToList();
        var byNameAsId = (await _store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).ToList();
        var byId = await _store.FindAsync(new RoleFilter { Id = "role-generated-1" });

        Assert.Equal(["role-generated-1"], byIds.Select(x => x.Id));
        Assert.Empty(byNameAsId);
        Assert.NotNull(byId);
        Assert.Equal("admin", byId.Name);
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
        Assert.IsType<InvalidOperationException>(exception);

        using var tenantARead = _tenantAccessor.PushContext(TenantA());
        var stored = await _store.FindAsync(new RoleFilter { Id = "admin" });

        Assert.NotNull(stored);
        Assert.Equal("tenant-a", stored.TenantId);
        Assert.Equal(["perm-a-updated"], stored.Permissions);
        Assert.Equal("admin", stored.Name);
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private static Tenant TenantA() => new() { Id = "tenant-a" };
    private static Tenant TenantB() => new() { Id = "tenant-b" };

    private static Role Role(string id, string name, string permission) => new()
    {
        Id = id,
        Name = name,
        Permissions = [permission]
    };

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
