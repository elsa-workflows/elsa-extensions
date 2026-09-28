using Elsa.Extensions;
using Elsa.Mcp.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Elsa.Mcp.Server.UnitTests;

/// <summary>
/// The seam a host adds its own tool types through. Without it a host would have to call <c>AddMcpServer</c> a second
/// time, registering the transport and both request handlers again.
/// </summary>
public class McpServerFeatureToolsTests
{
    [Fact]
    public void OffersAToolTypeTheHostRegisteredThroughConfigureTools()
    {
        using ServiceProvider services = Host(feature => feature.ConfigureTools = builder => builder.WithTools<ProbeTools>());

        Assert.Contains("probe", ToolNames(services));
    }

    [Fact]
    public void KeepsTheBuiltInInstanceToolsNextToTheHostsOwn()
    {
        using ServiceProvider services = Host(feature => feature.ConfigureTools = builder => builder.WithTools<ProbeTools>());

        Assert.Contains("search_workflow_instances", ToolNames(services));
    }

    [Fact]
    public void RegistersNothingExtraWhenTheHostConfiguresNoTools()
    {
        using ServiceProvider services = Host(_ => { });

        Assert.DoesNotContain("probe", ToolNames(services));
    }

    private static IEnumerable<string> ToolNames(IServiceProvider services) =>
        services.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name);

    private static ServiceProvider Host(Action<McpServerFeature> configure)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseMcpServer(configure));

        return services.BuildServiceProvider();
    }

    [McpServerToolType]
    private sealed class ProbeTools
    {
        private ProbeTools() { }

        [McpServerTool(Name = "probe")]
        public static string Probe() => "ok";
    }
}
