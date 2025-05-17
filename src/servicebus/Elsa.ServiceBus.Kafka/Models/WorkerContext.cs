using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.Kafka;

public record WorkerContext(IServiceScopeFactory ScopeFactory, ConsumerDefinition ConsumerDefinition);