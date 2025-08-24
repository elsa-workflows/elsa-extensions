using Elsa.Testing.Core.Entities;

namespace Elsa.Testing.Core.Filters;

public class TestScenarioFilter
{
    public string? Id { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    
    public IQueryable<TestScenario> Apply(IQueryable<TestScenario> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (WorkflowDefinitionId != null) queryable = queryable.Where(x => x.WorkflowDefinitionId == WorkflowDefinitionId);

        return queryable;
    }
}