using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.AzureServiceBus.ShellFeatures;

/// <summary>
/// Shell feature for Azure Service Bus message bus.
/// </summary>
[ShellFeature(
    DisplayName = "Azure Service Bus",
    Description = "Enables Azure Service Bus for message publishing and handling")]
[UsedImplicitly]
public class AzureServiceBusShellFeature : IShellFeature
{
    public string ConnectionStringOrName { get; set; } = string.Empty;

    public void ConfigureServices(IServiceCollection services)
    {
        // Service registration for Azure Service Bus
        services.AddOptions<Options.AzureServiceBusOptions>()
            .Configure(options => options.ConnectionStringOrName = ConnectionStringOrName);
    }
}

