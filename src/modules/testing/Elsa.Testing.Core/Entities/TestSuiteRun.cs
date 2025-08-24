using Elsa.Common.Entities;
using Elsa.Testing.Core.Models;

namespace Elsa.Testing.Core.Entities;

public class TestSuiteRun : Entity
{
    public string TestSuiteId { get; set; } = null!;
    public TestRunStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}