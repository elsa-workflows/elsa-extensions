namespace Elsa.Testing.Core.Options;

public class TestingOptions
{
    public ISet<Type> AssertionTypes { get; set; } = new HashSet<Type>();
}