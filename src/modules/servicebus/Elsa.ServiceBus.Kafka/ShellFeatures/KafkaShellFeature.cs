using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.Kafka.ShellFeatures;

/// <summary>
/// Shell feature for Kafka message bus.
/// </summary>
[ShellFeature(
    DisplayName = "Kafka Service Bus",
    Description = "Enables Apache Kafka for message publishing and handling")]
[UsedImplicitly]
public class KafkaShellFeature : IShellFeature
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public void ConfigureServices(IServiceCollection services)
    {
        // Kafka configuration setup
        services.AddOptions<Elsa.ServiceBus.Kafka.Options.KafkaOptions>()
            .Configure(options => options.BootstrapServers = BootstrapServers);
    }
}

