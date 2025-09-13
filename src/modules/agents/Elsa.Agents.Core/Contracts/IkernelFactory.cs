using Microsoft.SemanticKernel;
namespace Elsa.Agents;

public interface IKernelFactory
{
    Kernel CreateKernel(KernelConfig kernelConfig, AgentConfig agentConfig);
    Kernel CreateKernel(KernelConfig kernelConfig, string agentName);
}