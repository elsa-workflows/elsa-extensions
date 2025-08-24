using Elsa.Common.Services;
using Elsa.Testing.Core.Contracts;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;
using JetBrains.Annotations;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;

namespace Elsa.Testing.Core.Services;

[UsedImplicitly]
public class MemoryTestScenarioStore(MemoryStore<TestScenario> memoryStore) : ITestScenarioStore
{
    public Task<TestScenario?> FindAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default)
    {
        var result = Filter(memoryStore.List().AsQueryable(), filter).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<Page<TestScenario>> ListAsync<TOrderBy>(TestScenarioFilter filter, PageArgs pageArgs, OrderDefinition<TestScenario, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = memoryStore.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = memoryStore.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    public Task AddAsync(TestScenario entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TestScenario entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default)
    {
        var items = Filter(memoryStore.List().AsQueryable(), filter).ToList();
        memoryStore.DeleteMany(items, x => x.Id);
        return Task.FromResult((long)items.Count);
    }

    private IQueryable<TestScenario> Filter(IQueryable<TestScenario> queryable, TestScenarioFilter filter)
    {
        if (filter.Id != null)
            queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.WorkflowDefinitionId != null)
            queryable = queryable.Where(x => x.WorkflowDefinitionId == filter.WorkflowDefinitionId);
        return queryable;
    }
}