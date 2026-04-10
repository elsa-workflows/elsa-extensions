using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Elsa.ServiceBus.Kafka.Implementations;

namespace Elsa.ServiceBus.Kafka.Factories;

/// <summary>
/// A consumer factory that deserializes Avro-encoded messages using a Confluent Schema Registry.
/// The deserialized value will be a <see cref="GenericRecord"/> whose <c>Schema.Fullname</c> can be
/// matched against the <c>SchemaFullName</c> filter on the <see cref="Activities.MessageReceived"/> activity.
/// </summary>
public class AvroConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var schemaRegistryDefinition = context.SchemaRegistryDefinition
            ?? throw new InvalidOperationException(
                $"{nameof(AvroConsumerFactory)} requires a {nameof(SchemaRegistryDefinition)} " +
                $"to be configured on the consumer definition.");

        ISchemaRegistryClient schemaRegistryClient = BuildSchemaRegistryClient(schemaRegistryDefinition, context.ConsumerDefinition.Config);
        IAsyncDeserializer<GenericRecord> avroDeserializer = new AvroDeserializer<GenericRecord>(schemaRegistryClient);

        var consumer = new ConsumerBuilder<string, GenericRecord>(context.ConsumerDefinition.Config)
            .SetValueDeserializer(new SyncDeserializerAdapter(avroDeserializer))
            .Build();

        return new ConsumerProxy(consumer);
    }

    /// <summary>
    /// Builds a <see cref="CachedSchemaRegistryClient"/> for the given registry definition.
    /// When <see cref="AuthCredentialsSource.SaslInherit"/> is configured, the Kafka SASL credentials
    /// are bridged into the registry config, because <see cref="CachedSchemaRegistryClient"/> resolves
    /// them from its own config dictionary rather than from the Kafka client config.
    /// </summary>
    private static ISchemaRegistryClient BuildSchemaRegistryClient(SchemaRegistryDefinition def, ConsumerConfig kafkaConfig)
    {
        var registryConfig = def.Config;

        if (registryConfig.BasicAuthCredentialsSource != AuthCredentialsSource.SaslInherit
            || string.IsNullOrEmpty(kafkaConfig.SaslUsername))
            return new CachedSchemaRegistryClient(registryConfig);

        // CachedSchemaRegistryClient(IEnumerable<KeyValuePair<string,string>>) reads "sasl.username"
        // and "sasl.password" directly from the config entries when SaslInherit is set.
        // We merge them in from the Kafka config without mutating the shared SchemaRegistryDefinition.
        var merged = registryConfig
            .Where(e => e.Key is not "sasl.username" and not "sasl.password")
            .Append(new KeyValuePair<string, string>("sasl.username", kafkaConfig.SaslUsername))
            .Append(new KeyValuePair<string, string>("sasl.password", kafkaConfig.SaslPassword ?? ""));

        return new CachedSchemaRegistryClient(merged);
    }

    /// <summary>
    /// Adapts <see cref="IAsyncDeserializer{T}"/> to the synchronous <see cref="IDeserializer{T}"/> interface
    /// expected by <see cref="ConsumerBuilder{TKey,TValue}"/>.
    /// Blocking is acceptable here because schema lookups are cached by <see cref="CachedSchemaRegistryClient"/>
    /// and the consumer runs on a dedicated background thread.
    /// </summary>
    private sealed class SyncDeserializerAdapter(IAsyncDeserializer<GenericRecord> inner) : IDeserializer<GenericRecord>
    {
        public GenericRecord Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return inner.DeserializeAsync(data.ToArray(), isNull, context).GetAwaiter().GetResult();
        }
    }
}
