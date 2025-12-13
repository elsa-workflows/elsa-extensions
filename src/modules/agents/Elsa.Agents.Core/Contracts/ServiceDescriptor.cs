using Microsoft.SemanticKernel;

namespace Elsa.Agents;

public class ServiceDescriptor
{
    public string Name { get; set; } = null!;
    public Action<IKernelBuilder> ConfigureKernel { get; set; } = null!;
}