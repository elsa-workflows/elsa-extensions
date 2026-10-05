using Elsa.Identity.Entities;
using Elsa.Persistence.MongoDb.Modules.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Elsa.MongoDb.UnitTests;

/// <summary>
/// Drop-and-create of the Role name unique index against a 3.9.0-shaped database (elsa-core#8615).
/// The old store-wide unique index is <c>Name_1</c>; the replacement is the compound unique
/// <c>TenantId_1_Name_1</c>. User.Name and Application.Name/ClientId indexes are left alone.
/// </summary>
public sealed class MongoRoleIndexMigrationTests : IClassFixture<MongoRoleIndexMigrationTests.MongoFixture>, IDisposable
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Role> _roles;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Application> _applications;

    public MongoRoleIndexMigrationTests(MongoFixture fixture)
    {
        _client = new MongoClient(fixture.Container.GetConnectionString());
        _database = _client.GetDatabase($"elsa-role-indexes-{Guid.NewGuid():N}");
        _roles = _database.GetCollection<Role>("roles");
        _users = _database.GetCollection<User>("users");
        _applications = _database.GetCollection<Application>("applications");
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task ExistingData_DropAndCreate_IsIdempotent_AndAllowsSameNameAcrossTenants()
    {
        await SeedVersion390ShapeAsync();

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "Name_1", "TenantId_1"]));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_users));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_applications));
        Assert.Contains("ClientId_1", await ListIndexNamesAsync(_applications));

        var logger = new CollectingLogger();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.DoesNotContain(IdentityRoleIndexes.LegacyNameUnique, await ListIndexNamesAsync(_roles));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_users));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_applications));
        Assert.Contains("ClientId_1", await ListIndexNamesAsync(_applications));
        Assert.Contains(logger.Messages, m => m.Contains("Dropped", StringComparison.Ordinal) && m.Contains(IdentityRoleIndexes.LegacyNameUnique, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, m => m.Contains("Created", StringComparison.Ordinal) && m.Contains(IdentityRoleIndexes.TenantIdNameUnique, StringComparison.Ordinal));

        await _roles.InsertOneAsync(Role("role-b-admin", "admin", "tenant-b", "perm-b"));

        var tenantARoles = await _roles.Find(x => x.Name == "admin").ToListAsync();
        Assert.Equal(2, tenantARoles.Count);
        Assert.Equal(["tenant-a", "tenant-b"], tenantARoles.Select(x => x.TenantId).Order());

        var duplicate = await Record.ExceptionAsync(() =>
            _roles.InsertOneAsync(Role("role-a-dup", "admin", "tenant-a", "perm-dup")));
        Assert.NotNull(duplicate);
        Assert.True(IsDuplicateKey(duplicate), duplicate.ToString());

        logger.Messages.Clear();
        await RunCreateIndicesAsync(logger);

        Assert.True((await ListIndexNamesAsync(_roles)).SetEquals(["_id_", "TenantId_1", "TenantId_1_Name_1"]));
        Assert.Contains("Name_1", await ListIndexNamesAsync(_users));
        Assert.Contains("ClientId_1", await ListIndexNamesAsync(_applications));
        Assert.Contains(logger.Messages, m => m.Contains("was not found", StringComparison.Ordinal) && m.Contains(IdentityRoleIndexes.LegacyNameUnique, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, m => m.Contains("already present", StringComparison.Ordinal) && m.Contains(IdentityRoleIndexes.TenantIdNameUnique, StringComparison.Ordinal));
    }

    private async Task SeedVersion390ShapeAsync()
    {
        await _roles.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Role>(Builders<Role>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Role>(Builders<Role>.IndexKeys.Ascending(x => x.TenantId))
        ]);
        await _users.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.TenantId))
        ]);
        await _applications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.ClientId), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.Name), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Application>(Builders<Application>.IndexKeys.Ascending(x => x.TenantId))
        ]);

        await _roles.InsertOneAsync(Role("admin", "admin", "tenant-a", "perm-a"));
        await _roles.InsertOneAsync(Role("power-user", "power-user", "tenant-b", "perm-b"));
        await _users.InsertOneAsync(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" });
        await _applications.InsertOneAsync(new Application
        {
            Id = "app-a",
            Name = "console",
            ClientId = "console-client",
            TenantId = "tenant-a"
        });
    }

    private async Task RunCreateIndicesAsync(CollectingLogger logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_roles);
        services.AddSingleton(_users);
        services.AddSingleton(_applications);
        services.AddSingleton<ILogger<CreateIndices>>(logger);
        await using var provider = services.BuildServiceProvider();
        var hosted = new CreateIndices(provider);
        await hosted.StartAsync(CancellationToken.None);
    }

    private static async Task<HashSet<string>> ListIndexNamesAsync<T>(IMongoCollection<T> collection)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var cursor = await collection.Indexes.ListAsync();
        foreach (var index in (await cursor.ToListAsync())
                 .Where(index => index.TryGetValue("name", out var name) && name.BsonType == BsonType.String))
            names.Add(index["name"].AsString);

        return names;
    }

    private static bool IsDuplicateKey(Exception exception) =>
        exception switch
        {
            MongoWriteException write => write.WriteError.Category == ServerErrorCategory.DuplicateKey || write.WriteError.Code == 11000,
            MongoCommandException command => command.Code == 11000,
            MongoBulkWriteException bulk => bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey || error.Code == 11000),
            _ => exception.InnerException is { } inner && IsDuplicateKey(inner)
        };

    private static Role Role(string id, string name, string tenantId, string permission) => new()
    {
        Id = id,
        Name = name,
        TenantId = tenantId,
        Permissions = [permission]
    };

    public sealed class MongoFixture : IAsyncLifetime
    {
        public MongoDbContainer Container { get; } = new MongoDbBuilder().WithImage("mongo:7.0.24").Build();

        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();
    }

    private sealed class CollectingLogger : ILogger<CreateIndices>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
