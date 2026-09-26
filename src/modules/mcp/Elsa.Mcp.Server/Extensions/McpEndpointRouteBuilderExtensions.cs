using Elsa.Mcp.Server.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IEndpointRouteBuilder"/> with methods to map the Model Context Protocol endpoints.
/// </summary>
public static class McpEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the MCP endpoints on the configured route and applies the configured authorization.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpServer(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        McpOptions options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;
        IEndpointConventionBuilder builder = endpoints.MapMcp(options.Route);

        if (!options.RequireAuthorization)
            return endpoints;

        if (options.IsAuthorizationDiscoveryEnabled)
            builder.RequireAuthorization(BuildDiscoveryPolicy(endpoints, options));
        else if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            builder.RequireAuthorization();
        else
            builder.RequireAuthorization(options.AuthorizationPolicy);

        return endpoints;
    }

    /// <summary>
    /// Builds the policy used when OAuth discovery is enabled. A policy challenges the schemes it names, so the MCP
    /// scheme has to be the one named for the challenge to carry the resource metadata. The configured policy keeps
    /// its requirements; only the scheme answering an unauthenticated call changes.
    /// </summary>
    private static AuthorizationPolicy BuildDiscoveryPolicy(IEndpointRouteBuilder endpoints, McpOptions options)
    {
        AuthorizationPolicyBuilder policyBuilder = new();

        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            policyBuilder.RequireAuthenticatedUser();
        }
        else
        {
            IAuthorizationPolicyProvider policyProvider = endpoints.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();
            AuthorizationPolicy policy = policyProvider.GetPolicyAsync(options.AuthorizationPolicy).GetAwaiter().GetResult()
                ?? throw new InvalidOperationException($"The authorization policy '{options.AuthorizationPolicy}' configured for the MCP endpoints was not found.");

            policyBuilder.Combine(policy);
        }

        policyBuilder.AuthenticationSchemes = [McpAuthenticationDefaults.AuthenticationScheme];

        return policyBuilder.Build();
    }
}
