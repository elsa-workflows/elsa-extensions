using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Models;

namespace Elsa.Testing.Core.Filters;

public class TestSuiteRunFilter
{
    public string? Id { get; set; }
    public string? TestSuiteId { get; set; }
    public TestRunStatus? Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public IQueryable<TestSuiteRun> Apply(IQueryable<TestSuiteRun> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (TestSuiteId != null) queryable = queryable.Where(x => x.TestSuiteId == TestSuiteId);
        if (Status != null) queryable = queryable.Where(x => x.Status == Status);
        if (StartedAt != null) queryable = queryable.Where(x => x.StartedAt == StartedAt);
        if (FinishedAt != null) queryable = queryable.Where(x => x.FinishedAt == FinishedAt);
        return queryable;
    }
}
