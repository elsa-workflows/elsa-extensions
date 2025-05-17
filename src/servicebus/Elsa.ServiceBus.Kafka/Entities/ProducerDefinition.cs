using Confluent.Kafka;
using Elsa.Common.Entities;

namespace Elsa.ServiceBus.Kafka;

public class ProducerDefinition : Entity
{
    public string Name { get; set; } = default!;
    public Type FactoryType { get; set; } = default!;
    public ProducerConfig Config { get; set; } = new();
    public string? SchemaRegistryId { get; set; } = default!;
}