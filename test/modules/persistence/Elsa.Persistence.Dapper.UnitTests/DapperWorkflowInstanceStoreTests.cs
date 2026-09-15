using Dapper;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;
using Microsoft.Data.Sqlite;
using NSubstitute;

namespace Elsa.Persistence.Dapper.UnitTests;

public sealed class DapperWorkflowInstanceStoreTests : IDisposable
{
    private static readonly DateTimeOffset Cutoff = new(2026, 1, 1, 12, 5, 0, TimeSpan.Zero);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-dapper-instances-{Guid.NewGuid():N}.db");
    private readonly DapperWorkflowInstanceStore _store;
    private readonly TestTenantAccessor _tenantAccessor = new();

    public DapperWorkflowInstanceStoreTests()
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString();
        var connectionProvider = new SqliteDbConnectionProvider(connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
                           create table WorkflowInstances (
                               Id text not null primary key,
                               TenantId text null,
                               DefinitionId text not null,
                               DefinitionVersionId text not null,
                               Version integer not null,
                               WorkflowState text not null,
                               Status text not null,
                               SubStatus text not null,
                               IsExecuting integer not null,
                               CorrelationId text null,
                               Name text null,
                               IncidentCount integer not null,
                               IsSystem integer not null,
                               CreatedAt text not null,
                               UpdatedAt text null,
                               FinishedAt text null
                           );
                           """);

        // Seeded through parameters rather than literals so the driver writes the timestamps in
        // exactly the format it later binds them in for comparison.
        Insert(connection, "stale-executing", Cutoff.AddMinutes(-5), isExecuting: true);
        Insert(connection, "stale-idle", Cutoff.AddMinutes(-1), isExecuting: false);
        Insert(connection, "at-cutoff", Cutoff, isExecuting: true);
        Insert(connection, "fresh-executing", Cutoff.AddMinutes(4), isExecuting: true);

        var store = new Store<WorkflowInstanceRecord>(connectionProvider, _tenantAccessor, "WorkflowInstances");
        _store = new DapperWorkflowInstanceStore(store, Substitute.For<IWorkflowStateSerializer>());
    }

    [Fact]
    public async Task FindManyIdsAsync_WithBeforeLastUpdated_ExcludesInstancesUpdatedAtOrAfterTheCutoff()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var ids = await _store.FindManyIdsAsync(new WorkflowInstanceFilter { BeforeLastUpdated = Cutoff });

        Assert.Equal(["stale-executing", "stale-idle"], ids.Order());
    }

    /// <summary>
    /// Mirrors the filter used by RestartInterruptedWorkflowsTask. An instance that is executing right
    /// now must not be reported as interrupted, or it gets restarted from the beginning mid-flight.
    /// </summary>
    [Fact]
    public async Task FindManyIdsAsync_WithBeforeLastUpdatedAndIsExecuting_ExcludesInstancesThatAreStillActive()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var ids = await _store.FindManyIdsAsync(new WorkflowInstanceFilter
        {
            IsExecuting = true,
            BeforeLastUpdated = Cutoff
        });

        Assert.Equal(["stale-executing"], ids);
    }

    [Fact]
    public async Task FindManyIdsAsync_WithoutBeforeLastUpdated_ReturnsAllInstances()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var ids = await _store.FindManyIdsAsync(new WorkflowInstanceFilter());

        Assert.Equal(["at-cutoff", "fresh-executing", "stale-executing", "stale-idle"], ids.Order());
    }

    [Fact]
    public async Task CountAsync_WithBeforeLastUpdated_CountsOnlyInstancesUpdatedBeforeTheCutoff()
    {
        using var tenantScope = _tenantAccessor.PushContext(new Tenant { Id = "tenant-a" });

        var count = await _store.CountAsync(new WorkflowInstanceFilter { BeforeLastUpdated = Cutoff });

        Assert.Equal(2, count);
    }

    public void Dispose()
    {
        File.Delete(_databasePath);
    }

    private static void Insert(SqliteConnection connection, string id, DateTimeOffset updatedAt, bool isExecuting)
    {
        connection.Execute(
            """
            insert into WorkflowInstances
                (Id, TenantId, DefinitionId, DefinitionVersionId, Version, WorkflowState, Status, SubStatus, IsExecuting, IncidentCount, IsSystem, CreatedAt, UpdatedAt)
            values
                (@Id, 'tenant-a', 'definition-1', 'definition-1:1', 1, '{}', 'Running', 'Executing', @IsExecuting, 0, 0, @CreatedAt, @UpdatedAt);
            """,
            new
            {
                Id = id,
                IsExecuting = isExecuting,
                CreatedAt = updatedAt,
                UpdatedAt = updatedAt
            });
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        public string TenantId => Tenant?.Id ?? Elsa.Common.Multitenancy.Tenant.DefaultTenantId;
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
