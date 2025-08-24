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
public class MemoryTestRunStore(MemoryStore<TestRun> memoryStore) : ITestRunStore
{
    public Task<TestRun?> FindAsync(TestRunFilter filter, CancellationToken cancellationToken = default)
    {
        var result = Filter(memoryStore.List().AsQueryable(), filter).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<Page<TestRun>> ListAsync<TOrderBy>(TestRunFilter filter, PageArgs pageArgs, OrderDefinition<TestRun, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = memoryStore.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = memoryStore.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    public Task AddAsync(TestRun entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TestRun entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(TestRunFilter filter, CancellationToken cancellationToken = default)
    {
        var items = Filter(memoryStore.List().AsQueryable(), filter).ToList();
        memoryStore.DeleteMany(items, x => x.Id);
        return Task.FromResult((long)items.Count);
    }

    private IQueryable<TestRun> Filter(IQueryable<TestRun> queryable, TestRunFilter filter)
    {
        return filter.Apply(queryable);
    }
}

