using System.Linq;
using Elsa.Identity.Entities;
using Elsa.Persistence.MongoDb.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Modules.Identity;

internal static class IdentityRoleIndexes
{
    /// <summary>
    /// Store-wide unique index on <see cref="Role.Name"/> created by 3.9.0 and earlier.
    /// </summary>
    public const string LegacyNameUnique = "Name_1";

    /// <summary>
    /// Per-tenant unique index on (TenantId, Name).
    /// </summary>
    public const string TenantIdNameUnique = "TenantId_1_Name_1";
}

internal class CreateIndices(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        return Task.WhenAll(
            CreateApplicationIndices(scope, cancellationToken),
            CreateUserIndices(scope, cancellationToken),
            CreateRoleIndices(scope, cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Task CreateApplicationIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var applicationCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<Application>>();
        if (applicationCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            applicationCollection,
            async (collection, indexBuilder) =>
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Application>>
                    {
                        new(indexBuilder.Ascending(x => x.ClientId), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.Name), new CreateIndexOptions
                        {
                            Unique = true
                        }),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken));
    }

    private static Task CreateUserIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var userCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<User>>();
        if (userCollection == null) return Task.CompletedTask;

        return IndexHelpers.CreateAsync(
            userCollection,
            async (collection, indexBuilder) =>
            {
                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<User>>
                    {
                        new(indexBuilder.Ascending(x => x.Name),
                            new CreateIndexOptions
                            {
                                Unique = true
                            }),
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken);
            });
    }

    private static Task CreateRoleIndices(IServiceScope serviceScope, CancellationToken cancellationToken)
    {
        var roleCollection = serviceScope.ServiceProvider.GetService<IMongoCollection<Role>>();
        if (roleCollection == null) return Task.CompletedTask;

        var logger = serviceScope.ServiceProvider.GetService<ILogger<CreateIndices>>() ?? NullLogger<CreateIndices>.Instance;

        return IndexHelpers.CreateAsync(
            roleCollection,
            async (collection, indexBuilder) =>
            {
                var existingNames = await ListIndexNamesAsync(collection, cancellationToken);
                if (existingNames.Contains(IdentityRoleIndexes.TenantIdNameUnique))
                {
                    logger.LogDebug("Role unique index '{IndexName}' is already present.", IdentityRoleIndexes.TenantIdNameUnique);
                }
                else
                {
                    await collection.Indexes.CreateOneAsync(
                        new CreateIndexModel<Role>(
                            indexBuilder.Ascending(x => x.TenantId).Ascending(x => x.Name),
                            new CreateIndexOptions
                            {
                                Unique = true,
                                Name = IdentityRoleIndexes.TenantIdNameUnique
                            }),
                        cancellationToken: cancellationToken);
                    logger.LogDebug("Created Role unique index '{IndexName}' on (TenantId, Name).", IdentityRoleIndexes.TenantIdNameUnique);
                }

                // Create the compound unique index before dropping Name_1 so two nodes
                // starting together never leave the collection without name uniqueness,
                // and a second DropOne of Name_1 is IndexNotFound (code 27), not startup failure.
                try
                {
                    await collection.Indexes.DropOneAsync(IdentityRoleIndexes.LegacyNameUnique, cancellationToken);
                    logger.LogInformation("Dropped Role unique index '{IndexName}'.", IdentityRoleIndexes.LegacyNameUnique);
                }
                catch (MongoCommandException exception) when (exception.Code == 27 || exception.InnerException is MongoCommandException { Code: 27 })
                {
                    logger.LogDebug("Role unique index '{IndexName}' was not found.", IdentityRoleIndexes.LegacyNameUnique);
                }

                await collection.Indexes.CreateManyAsync(
                    new List<CreateIndexModel<Role>>
                    {
                        new(indexBuilder.Ascending(x => x.TenantId))
                    },
                    cancellationToken);
            });
    }

    private static async Task<HashSet<string>> ListIndexNamesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var cursor = await collection.Indexes.ListAsync(cancellationToken);
        foreach (var index in (await cursor.ToListAsync(cancellationToken))
                 .Where(index => index.TryGetValue("name", out var name) && name.BsonType == BsonType.String))
            names.Add(index["name"].AsString);

        return names;
    }
}
