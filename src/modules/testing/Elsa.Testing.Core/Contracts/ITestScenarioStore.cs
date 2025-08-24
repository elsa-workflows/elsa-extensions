using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Filters;

namespace Elsa.Testing.Core.Contracts;

public interface ITestScenarioStore
{
    Task<TestScenario?> FindAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default);
    Task<Page<TestScenario>> ListAsync<TOrderBy>(TestScenarioFilter filter, PageArgs pageArgs, OrderDefinition<TestScenario, TOrderBy> order, CancellationToken cancellationToken = default);
    Task AddAsync(TestScenario entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TestScenario entity, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(TestScenarioFilter filter, CancellationToken cancellationToken = default);
}