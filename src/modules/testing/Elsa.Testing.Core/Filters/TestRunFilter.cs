using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Models;

namespace Elsa.Testing.Core.Filters;

public class TestRunFilter
{
    public string? Id { get; set; }
    public string? TestScenarioId { get; set; }
    public string? TestSuiteRunId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public TestRunStatus? Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public IQueryable<TestRun> Apply(IQueryable<TestRun> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (TestScenarioId != null) queryable = queryable.Where(x => x.TestScenarioId == TestScenarioId);
        if (TestSuiteRunId != null) queryable = queryable.Where(x => x.TestSuiteRunId == TestSuiteRunId);
        if (WorkflowInstanceId != null) queryable = queryable.Where(x => x.WorkflowInstanceId == WorkflowInstanceId);
        if (Status != null) queryable = queryable.Where(x => x.Status == Status);
        if (StartedAt != null) queryable = queryable.Where(x => x.StartedAt == StartedAt);
        if (FinishedAt != null) queryable = queryable.Where(x => x.FinishedAt == FinishedAt);
        return queryable;
    }
}
