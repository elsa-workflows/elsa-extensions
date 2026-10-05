using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Persistence.Dapper.Migrations.Identity;
using Elsa.Persistence.Dapper.Modules.Identity.Records;
using Elsa.Persistence.Dapper.Modules.Identity.Stores;
using Elsa.Persistence.Dapper.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// V3.10 unique index on Roles (TenantId, Name) against a 3.9-shaped SQLite database (#282).
/// </summary>
public sealed class DapperRoleNameUniquenessMigrationTests : IDisposable
{
    private const long IdentityV33 = 30004;
    private readonly string _databaseFileName = $"elsa-dapper-role-ux-{Guid.NewGuid():N}.db";
    private readonly string _databasePath;
    private readonly string _connectionString;

    public DapperRoleNameUniquenessMigrationTests()
    {
        _databasePath = Path.Join(Path.GetTempPath(), _databaseFileName);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
    }

    [Fact]
    public async Task Existing39Data_TwoTenantsSameName_MigratesAndKeepsLegacyIds()
    {
        MigrateUp(IdentityV33);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("role-b-admin", "admin", "tenant-b", "perm-b");
        InsertRole("power-user", "power-user", "tenant-b", "perm-pu");

        MigrateUp();

        Assert.True(IndexExists());
        Assert.Equal(
            [
                ("admin", "admin", "tenant-a"),
                ("power-user", "power-user", "tenant-b"),
                ("role-b-admin", "admin", "tenant-b")
            ],
            LoadRoles());

        var tenantAccessor = new TestTenantAccessor();
        var store = CreateStore(tenantAccessor);

        using (var tenantA = tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            var legacy = await store.FindAsync(new RoleFilter { Id = "admin" });
            var byIds = (await store.FindManyAsync(new RoleFilter { Ids = ["admin"] })).ToList();
            Assert.NotNull(legacy);
            Assert.Equal("admin", legacy.Name);
            Assert.Equal(["admin"], byIds.Select(x => x.Id));
        }

        using var tenantB = tenantAccessor.PushContext(new Tenant { Id = "tenant-b" });
        var tenantBAdmin = await store.FindAsync(new RoleFilter { Id = "role-b-admin" });
        Assert.NotNull(tenantBAdmin);
        Assert.Equal("admin", tenantBAdmin.Name);
    }

    [Fact]
    public void SameTenantExactDuplicates_FailTheMigration_AndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV33);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("admin-dup", "admin", "tenant-a", "perm-dup");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        Assert.Contains(V3_10.TenantIdNameUniqueIndex, exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Id=admin", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Id=admin-dup", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("No rows were changed", exception.ToString(), StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.False(VersionApplied(30005));
        Assert.Equal(
            [
                ("admin", "admin", "tenant-a"),
                ("admin-dup", "admin", "tenant-a")
            ],
            LoadRoles());
    }

    [Fact]
    public void SameTenantCaseVariants_FailTheMigration_AndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV33);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("admin-cased", "Admin", "tenant-a", "perm-cased");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        Assert.Contains("Id=admin", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Id=admin-cased", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("'admin'", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("'Admin'", exception.ToString(), StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.Equal(
            [
                ("admin", "admin", "tenant-a"),
                ("admin-cased", "Admin", "tenant-a")
            ],
            LoadRoles());
    }

    [Fact]
    public void NullAndEmptyDefaultTenant_SameName_FailTheMigration_AndLeaveRowsUnchanged()
    {
        MigrateUp(IdentityV33);
        InsertRole("admin-null", "admin", null, "perm-null");
        InsertRole("admin-empty", "admin", "", "perm-empty");

        var exception = Record.Exception(() => MigrateUp());

        Assert.NotNull(exception);
        Assert.Contains("Id=admin-null", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Id=admin-empty", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("(default)", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("No rows were changed", exception.ToString(), StringComparison.Ordinal);
        Assert.False(IndexExists());
        Assert.False(VersionApplied(30005));
        Assert.Equal(
            [
                ("admin-empty", "admin", ""),
                ("admin-null", "admin", null)
            ],
            LoadRoles());
    }

    [Fact]
    public async Task AfterMigration_SameTenantDuplicateInsert_IsRejected()
    {
        MigrateUp(IdentityV33);
        InsertRole("admin", "admin", "tenant-a", "perm-a");
        InsertRole("role-b-admin", "admin", "tenant-b", "perm-b");
        MigrateUp();

        var tenantAccessor = new TestTenantAccessor();
        var store = CreateStore(tenantAccessor);

        using (var tenantA = tenantAccessor.PushContext(new Tenant { Id = "tenant-a" }))
        {
            var exception = await Record.ExceptionAsync(() =>
                store.SaveAsync(Role("role-a-dup", "admin", "perm-dup")));
            Assert.NotNull(exception);
        }

        using var connection = new SqliteConnection(_connectionString);
        var raw = await Record.ExceptionAsync(() =>
            connection.ExecuteAsync(
                "insert into Roles (Id, Name, Permissions, TenantId) values ('raw-dup', 'admin', 'x', 'tenant-a')"));
        Assert.NotNull(raw);

        Assert.Equal(
            [
                ("admin", "admin", "tenant-a"),
                ("role-b-admin", "admin", "tenant-b")
            ],
            LoadRoles());
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private DapperRoleStore CreateStore(ITenantAccessor tenantAccessor) =>
        new(new Store<RoleRecord>(new SqliteDbConnectionProvider(_connectionString), tenantAccessor, "Roles"));

    private void InsertRole(string id, string name, string? tenantId, string permissions)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(
            "insert into Roles (Id, Name, Permissions, TenantId) values (@id, @name, @permissions, @tenantId)",
            new { id, name, permissions, tenantId });
    }

    private IReadOnlyList<(string Id, string Name, string? TenantId)> LoadRoles()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.Query<(string Id, string Name, string? TenantId)>(
            "select Id, Name, TenantId from Roles order by Id").ToList();
    }

    private bool IndexExists()
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            "select count(*) from sqlite_master where type = 'index' and name = @name",
            new { name = V3_10.TenantIdNameUniqueIndex }) == 1;
    }

    private bool VersionApplied(long version)
    {
        using var connection = new SqliteConnection(_connectionString);
        return connection.ExecuteScalar<long>(
            "select count(*) from VersionInfo where Version = @version",
            new { version }) == 1;
    }

    private void MigrateUp(long? targetVersion = null)
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(Initial).Assembly).For.Migrations())
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        if (targetVersion is { } version)
            runner.MigrateUp(version);
        else
            runner.MigrateUp();
    }

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
