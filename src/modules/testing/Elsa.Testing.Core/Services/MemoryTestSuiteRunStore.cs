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
public class MemoryTestSuiteRunStore(MemoryStore<TestSuiteRun> memoryStore) : ITestSuiteRunStore
{
    public Task<TestSuiteRun?> FindAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default)
    {
        var result = Filter(memoryStore.List().AsQueryable(), filter).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<Page<TestSuiteRun>> ListAsync<TOrderBy>(TestSuiteRunFilter filter, PageArgs pageArgs, OrderDefinition<TestSuiteRun, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = memoryStore.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = memoryStore.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    public Task AddAsync(TestSuiteRun entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Add(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TestSuiteRun entity, CancellationToken cancellationToken = default)
    {
        memoryStore.Update(entity, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default)
    {
        var items = Filter(memoryStore.List().AsQueryable(), filter).ToList();
        memoryStore.DeleteMany(items, x => x.Id);
        return Task.FromResult((long)items.Count);
    }

    private IQueryable<TestSuiteRun> Filter(IQueryable<TestSuiteRun> queryable, TestSuiteRunFilter filter)
    {
        // Extend filtering logic as needed.
        return queryable;
    }
}

