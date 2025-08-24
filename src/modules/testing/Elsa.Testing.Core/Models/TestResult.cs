namespace Elsa.Testing.Core.Models;

public class TestResult
{
    public TestResultStatus Status { get; set; }
    public ICollection<AssertionResult> AssertionResults { get; set; } = [];
}