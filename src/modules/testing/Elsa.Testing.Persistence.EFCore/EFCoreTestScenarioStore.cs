using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Testing.Core.Contracts;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;
using Elsa.Testing.Core.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Testing.Persistence.EFCore;

/// <summary>
/// An EF Core implementation of <see cref="ITestScenarioStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreTestScenarioStore(EntityStore<TestingDbContext, TestScenario> store, AssertionSerializer assertionSerializer, ILogger<EFCoreTestScenarioStore> logger) : ITestScenarioStore
{
    public async Task<TestScenario?> FindAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    public async Task<Page<TestScenario>> ListAsync<TOrderBy>(TestScenarioFilter filter, PageArgs pageArgs, OrderDefinition<TestScenario, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, cancellationToken).LongCount();
        var scenarios = await store.QueryAsync(query => filter.Apply(query).Paginate(pageArgs).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
        return new(scenarios, count);
    }

    public async Task AddAsync(TestScenario entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, OnSaveAsync, cancellationToken);
    }

    public async Task UpdateAsync(TestScenario entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, OnSaveAsync, cancellationToken);
    }

    public async Task<long> DeleteAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }

    private ValueTask OnSaveAsync(TestingDbContext dbContext, TestScenario entity, CancellationToken cancellationToken)
    {
        dbContext.Entry(entity).Property("SerializedAssertions").CurrentValue = assertionSerializer.Serialize(entity.Assertions);
        
        return ValueTask.CompletedTask;
    }

    private ValueTask OnLoadAsync(TestingDbContext dbContext, TestScenario? entity, CancellationToken cancellationToken)
    {
        if (entity == null) return ValueTask.CompletedTask;
        
        var assertionsJson = (string?)dbContext.Entry(entity).Property("SerializedAssertions").CurrentValue;

        try
        {
            if (!string.IsNullOrWhiteSpace(assertionsJson))
                entity.Assertions = assertionSerializer.DeserializeMany(assertionsJson).ToList();
        }
        catch (Exception exp)
        {
            logger.LogError(exp, "Could not deserialize assertions for TestScenario {TestScenarioId}.", entity.Id);
        }
        
        return ValueTask.CompletedTask;
    }
}