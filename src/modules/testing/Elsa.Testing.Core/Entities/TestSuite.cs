using Elsa.Common.Entities;

namespace Elsa.Testing.Core.Entities;

public class TestSuite : Entity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<TestScenario> Scenarios { get; set; } = new List<TestScenario>();
}