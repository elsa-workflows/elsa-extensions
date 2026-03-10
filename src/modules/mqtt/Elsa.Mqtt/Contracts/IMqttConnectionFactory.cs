namespace Elsa.Mqtt.Contracts;

internal interface IMqttConnectionFactory
{
    /// <summary>
    /// Creates and binds an MQTT connection using the <paramref name="connectionName"/> for config retrieval.
    /// If no <paramref name="connectionName"/> is provided, the <c>Default</c> connection is used.
    /// </summary>
    public Task<IMqttConnection> CreateConnectionAsync(string? connectionName = null, CancellationToken cancellationToken = default);
}
