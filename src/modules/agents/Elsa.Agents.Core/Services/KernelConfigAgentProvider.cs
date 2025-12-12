namespace Elsa.Agents;

/// <summary>
/// Agent provider that exposes Semantic Kernel-configured agents via
/// <see cref="IKernelConfigProvider"/> and <see cref="AgentFrameworkFactory"/>.
/// </summary>
public class KernelConfigAgentProvider(
    IKernelConfigProvider kernelConfigProvider,
    AgentFrameworkFactory agentFrameworkFactory) : IAgentProvider
{
    public async Task<bool> CanProvideAsync(string name, CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        return kernelConfig.Agents.ContainsKey(name);
    }

    public async Task<IElsaAgent> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);

        if (!kernelConfig.Agents.TryGetValue(name, out var agentConfig))
            throw new InvalidOperationException($"Agent '{name}' not found in KernelConfig.");

        return agentFrameworkFactory.CreateElsaAgent(kernelConfig, agentConfig);
    }
}