using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;

namespace Elsa.Testing.Core.Contracts;

public interface ITestRunStore
{
    Task<TestRun?> FindAsync(TestRunFilter filter, CancellationToken cancellationToken = default);
    Task<Page<TestRun>> ListAsync<TOrderBy>(TestRunFilter filter, PageArgs pageArgs, OrderDefinition<TestRun, TOrderBy> order, CancellationToken cancellationToken = default);
    Task AddAsync(TestRun entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TestRun entity, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(TestRunFilter filter, CancellationToken cancellationToken = default);
}