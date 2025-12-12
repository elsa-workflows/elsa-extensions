using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

/// <summary>
/// Provides kernel configuration by merging configuration-based agents with code-first definitions.
/// </summary>
[UsedImplicitly]
public class ConfigurationKernelConfigProvider(IOptions<ConfiguredAgentOptions> options) : IKernelConfigProvider
{
    public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();

        // Add configuration-based items (if available)
        if (options.Value.ApiKeys != null!)
        {
            foreach (var apiKey in options.Value.ApiKeys)
                kernelConfig.ApiKeys[apiKey.Name] = apiKey;
        }

        if (options.Value.Services != null!)
        {
            foreach (var service in options.Value.Services)
                kernelConfig.Services[service.Name] = service;
        }

        if (options.Value.Agents != null!)
        {
            foreach (var agent in options.Value.Agents)
                kernelConfig.Agents[agent.Name] = agent;
        }

        return Task.FromResult(kernelConfig);
    }
}