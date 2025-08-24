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
/// An EF Core implementation of <see cref="ITestSuiteStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreTestSuiteStore(EntityStore<TestingDbContext, TestSuite> store) : ITestSuiteStore
{
    public async Task<TestSuite?> FindAsync(TestSuiteFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(filter.Apply, cancellationToken).FirstOrDefault();
    }

    public async Task<Page<TestSuite>> ListAsync<TOrderBy>(TestSuiteFilter filter, PageArgs pageArgs, OrderDefinition<TestSuite, TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(filter.Apply, cancellationToken).LongCount();
        var suites = await store.QueryAsync(query => filter.Apply(query).Paginate(pageArgs).OrderBy(order), cancellationToken).ToList();
        return new(suites, count);
    }

    public async Task AddAsync(TestSuite entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(TestSuite entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
    }

    public async Task<long> DeleteAsync(TestSuiteFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteWhereAsync(filter.Apply, cancellationToken);
    }
}

