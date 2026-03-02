using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Api.ShellFeatures;

/// <summary>
/// Shell feature for agents API.
/// </summary>
[ShellFeature(
    DisplayName = "Agents API",
    Description = "Exposes API endpoints for managing agents",
    DependsOn = ["AgentsCore"])]
[UsedImplicitly]
public class AgentsApiShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFastEndpointsAssembly<AgentsApiShellFeature>();
    }
}

