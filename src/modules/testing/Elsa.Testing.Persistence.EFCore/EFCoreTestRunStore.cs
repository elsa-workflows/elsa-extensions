using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Testing.Core.Contracts;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;
using JetBrains.Annotations;
using Open.Linq.AsyncExtensions;

namespace Elsa.Testing.Persistence.EFCore;

/// <summary>
/// An EF Core implementation of <see cref="ITestRunStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreTestRunStore(EntityStore<TestingDbContext, TestRun> store) : ITestRunStore
{
    public async Task<TestRun?> FindAsync(TestRunFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, cancellationToken).FirstOrDefault();
    }

    public async Task<Page<TestRun>> ListAsync<TOrderBy>(TestRunFilter filter, PageArgs pageArgs, OrderDefinition<TestRun, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, cancellationToken).LongCount();
        var runs = await store.QueryAsync(query => filter.Apply(query).Paginate(pageArgs).OrderBy(order), cancellationToken).ToList();
        return new(runs, count);
    }

    public async Task AddAsync(TestRun entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(TestRun entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
    }

    public async Task<long> DeleteAsync(TestRunFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }
}

