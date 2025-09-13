using Elsa.Abstractions;
using Elsa.Agents;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Agents.Api.Endpoints.ServiceProviders.List;

/// <summary>
/// Lists all registered service providers.
/// </summary>
[UsedImplicitly]
public class Endpoint(IServiceDiscoverer serviceDiscoverer) : ElsaEndpointWithoutRequest<ListResponse<string>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/ai/service-providers");
        ConfigurePermissions("ai/services:read");
    }

    /// <inheritdoc />
    public override Task<ListResponse<string>> ExecuteAsync(CancellationToken ct)
    {
        var providers = serviceDiscoverer.Discover().Select(x => x.Name).ToList();
        return Task.FromResult(new ListResponse<string>(providers));
    }
}
