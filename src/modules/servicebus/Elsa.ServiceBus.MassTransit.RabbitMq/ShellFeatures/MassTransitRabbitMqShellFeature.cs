using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.RabbitMq.ShellFeatures;

/// <summary>
/// Shell feature for RabbitMQ transport configuration with MassTransit.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit RabbitMQ Transport",
    Description = "Configures MassTransit to use RabbitMQ as the message transport",
    DependsOn = ["MassTransit Service Bus"])]
[UsedImplicitly]
public class MassTransitRabbitMqShellFeature : IShellFeature
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public void ConfigureServices(IServiceCollection services)
    {
        // Configuration would be applied via appsettings binding
        // The actual bus configuration happens in MassTransitShellFeature
    }
}

