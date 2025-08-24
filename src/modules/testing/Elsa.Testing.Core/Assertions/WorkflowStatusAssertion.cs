using Elsa.Testing.Core.Abstractions;
using Elsa.Testing.Core.Models;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Testing.Core.Assertions;

[UsedImplicitly]
public class WorkflowStatusAssertion : Assertion
{
    public WorkflowStatus ExpectedStatus { get; set; }
    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        var actualStatus = context.WorkflowInstance.Status;
        var statusesMatch = Equals(ExpectedStatus, actualStatus);
        var result = statusesMatch
            ? AssertionResult.Pass()
            : AssertionResult.Fail($"Expected workflow status to be '{ExpectedStatus}', but found '{actualStatus}'");
        return Task.FromResult(result);
    }
}