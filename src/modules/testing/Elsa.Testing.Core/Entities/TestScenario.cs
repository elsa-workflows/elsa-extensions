using Elsa.Common.Entities;
using Elsa.Testing.Core.Abstractions;

namespace Elsa.Testing.Core.Entities;

public class TestScenario : Entity
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string WorkflowDefinitionId { get; set; } = null!;
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Variables { get; set; }
    public ICollection<Assertion> Assertions { get; set; } = new List<Assertion>();
}