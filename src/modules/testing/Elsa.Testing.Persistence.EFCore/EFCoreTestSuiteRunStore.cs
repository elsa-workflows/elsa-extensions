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
/// An EF Core implementation of <see cref="ITestSuiteRunStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreTestSuiteRunStore(EntityStore<TestingDbContext, TestSuiteRun> store) : ITestSuiteRunStore
{
    public async Task<TestSuiteRun?> FindAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, cancellationToken).FirstOrDefault();
    }

    public async Task<Page<TestSuiteRun>> ListAsync<TOrderBy>(TestSuiteRunFilter filter, PageArgs pageArgs, OrderDefinition<TestSuiteRun, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, cancellationToken).LongCount();
        var suiteRuns = await store.QueryAsync(query => filter.Apply(query).Paginate(pageArgs).OrderBy(order), cancellationToken).ToList();
        return new(suiteRuns, count);
    }

    public async Task AddAsync(TestSuiteRun entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(TestSuiteRun entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
    }

    public async Task<long> DeleteAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }
}

