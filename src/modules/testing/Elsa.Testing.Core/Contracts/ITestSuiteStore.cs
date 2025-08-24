using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;

namespace Elsa.Testing.Core.Contracts;

public interface ITestSuiteStore
{
    Task<TestSuite?> FindAsync(TestSuiteFilter filter, CancellationToken cancellationToken = default);
    Task<Page<TestSuite>> ListAsync<TOrderBy>(TestSuiteFilter filter, PageArgs pageArgs, OrderDefinition<TestSuite, TOrderBy> order, CancellationToken cancellationToken = default);
    Task AddAsync(TestSuite entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TestSuite entity, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(TestSuiteFilter filter, CancellationToken cancellationToken = default);
}