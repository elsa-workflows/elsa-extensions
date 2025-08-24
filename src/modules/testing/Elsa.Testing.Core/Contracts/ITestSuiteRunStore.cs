using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;

namespace Elsa.Testing.Core.Contracts;

public interface ITestSuiteRunStore
{
    Task<TestSuiteRun?> FindAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default);
    Task<Page<TestSuiteRun>> ListAsync<TOrderBy>(TestSuiteRunFilter filter, PageArgs pageArgs, OrderDefinition<TestSuiteRun, TOrderBy> order, CancellationToken cancellationToken = default);
    Task AddAsync(TestSuiteRun entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TestSuiteRun entity, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(TestSuiteRunFilter filter, CancellationToken cancellationToken = default);
}