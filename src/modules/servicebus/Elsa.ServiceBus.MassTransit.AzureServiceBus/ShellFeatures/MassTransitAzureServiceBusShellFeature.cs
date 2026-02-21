using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.AzureServiceBus.ShellFeatures;

/// <summary>
/// Shell feature for Azure Service Bus transport configuration with MassTransit.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Azure Service Bus Transport",
    Description = "Configures MassTransit to use Azure Service Bus as the message transport",
    DependsOn = ["MassTransit Service Bus"])]
[UsedImplicitly]
public class MassTransitAzureServiceBusShellFeature : IShellFeature
{
    public string ConnectionString { get; set; } = string.Empty;

    public void ConfigureServices(IServiceCollection services)
    {
        // Configuration would be applied via appsettings binding
        // The actual bus configuration happens in MassTransitShellFeature
    }
}

