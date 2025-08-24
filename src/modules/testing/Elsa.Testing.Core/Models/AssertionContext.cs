using Elsa.Workflows.Management.Entities;

namespace Elsa.Testing.Core.Models;

public class AssertionContext
{
    public WorkflowInstance WorkflowInstance { get; init; } = null!;
}