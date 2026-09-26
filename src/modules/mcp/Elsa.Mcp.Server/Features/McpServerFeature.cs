using Elsa.Bookmarks.Ui.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mcp.Server.Configuration;
using Elsa.Mcp.Server.Handlers;
using Elsa.Mcp.Server.Services;
using Elsa.Mcp.Server.Tools;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace Elsa.Mcp.Server.Features;

/// <summary>
/// Represents a feature that exposes Elsa workflows as Model Context Protocol tools.
/// </summary>
public class McpServerFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Configures the MCP options.
    /// </summary>
    public Action<McpOptions>? ConfigureOptions { get; set; }

    /// <summary>
    /// Whether the built-in workflow instance tools are registered next to the workflow tools. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeWorkflowInstanceTools { get; set; } = true;

    /// <summary>
    /// Registers tool types of the host on the same builder the built-in tools use, for example
    /// <c>builder =&gt; builder.WithTools&lt;MyTools&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// A seam rather than a second <c>AddMcpServer</c> call in the host: that would register the HTTP transport and both
    /// request handlers again, and this feature owns all three.
    /// </remarks>
    public Action<IMcpServerBuilder>? ConfigureTools { get; set; }

    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply()
    {
        base.Apply();
        Services.AddHttpContextAccessor();
        Services.AddScoped<WorkflowToolCatalog>();
        Services.AddScoped<WorkflowToolInvoker>();

        // TryAdd, not Add: a host that wants to limit its callers plugs in its own implementation here with
        // Replace, and that has to work regardless of whether its feature is applied before or after this one.
        Services.TryAddScoped<ICallerTaskSearch, UnscopedCallerTaskSearch>();
        Services.AddBookmarkUi();
        Services.AddScoped<BookmarkDescriptionFactory>();

        IMcpServerBuilder builder = Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithListToolsHandler(WorkflowToolRequestHandlers.ListToolsAsync)
            .WithCallToolHandler(WorkflowToolRequestHandlers.CallToolAsync);

        // The workflow tools are discovered per request, so they live in the handlers above; the instance tools are
        // fixed and are registered as a regular tool type next to them.
        if (IncludeWorkflowInstanceTools)
            builder.WithTools<WorkflowInstanceTools>();

        ConfigureTools?.Invoke(builder);

        Services.Configure(ConfigureOptions ?? (_ => { }));

        AddAuthorizationDiscovery();
    }

    /// <summary>
    /// Registers the MCP authentication scheme. It owns the challenge on the MCP endpoints, so that an
    /// unauthenticated call is answered with a pointer to the protected resource metadata instead of a bare
    /// <c>Bearer</c>. The token itself is still validated by the host's scheme, which this scheme forwards to.
    /// </summary>
    private void AddAuthorizationDiscovery()
    {
        Services.AddAuthentication().AddMcp(_ => { });

        Services
            .AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<McpOptions>, IOptions<AuthenticationOptions>>(ConfigureAuthentication);
    }

    private static void ConfigureAuthentication(McpAuthenticationOptions authenticationOptions, IOptions<McpOptions> mcpOptions, IOptions<AuthenticationOptions> hostOptions)
    {
        McpOptions options = mcpOptions.Value;

        if (!options.IsAuthorizationDiscoveryEnabled)
        {
            // The scheme is registered either way, so its metadata endpoint has to step aside when there is nothing
            // to advertise. Left alone it answers with a 500 over metadata that was never configured.
            authenticationOptions.Events.OnResourceMetadataRequest = context =>
            {
                context.SkipHandler();
                return Task.CompletedTask;
            };

            return;
        }

        AuthenticationOptions host = hostOptions.Value;
        authenticationOptions.ForwardAuthenticate = options.AuthenticationScheme ?? host.DefaultAuthenticateScheme ?? host.DefaultScheme;
        authenticationOptions.ResourceMetadata = CreateResourceMetadata(options);

        // The resource identifier has to be the URL the client actually called - the server is reachable both
        // directly and through the gateway - so it is filled in per request rather than configured per environment.
        authenticationOptions.Events.OnResourceMetadataRequest = context =>
        {
            ProtectedResourceMetadata metadata = CreateResourceMetadata(options);
            metadata.Resource = $"{context.Request.Scheme}://{context.Request.Host}{options.Route}";
            context.ResourceMetadata = metadata;

            return Task.CompletedTask;
        };
    }

    private static ProtectedResourceMetadata CreateResourceMetadata(McpOptions options)
    {
        ProtectedResourceMetadata metadata = new()
        {
            ResourceName = options.ResourceName,
        };

        foreach (string authorizationServer in options.AuthorizationServers)
            metadata.AuthorizationServers.Add(authorizationServer);

        foreach (string scope in options.ScopesSupported)
            metadata.ScopesSupported.Add(scope);

        return metadata;
    }
}
