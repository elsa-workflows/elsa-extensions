using Elsa.Testing.Core.Models;

namespace Elsa.Testing.Core.Abstractions;

public abstract class Assertion
{
    public string Id { get; set; } = null!;
    public abstract Task<AssertionResult> RunAsync(AssertionContext context);
}