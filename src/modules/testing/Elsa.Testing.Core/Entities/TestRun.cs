using Elsa.Common.Entities;
using Elsa.Testing.Core.Models;

namespace Elsa.Testing.Core.Entities;

public class TestRun : Entity
{
    public string TestScenarioId { get; set; } = null!;
    public string? TestSuiteRunId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public TestRunStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public TestResult? Result { get; set; }
}