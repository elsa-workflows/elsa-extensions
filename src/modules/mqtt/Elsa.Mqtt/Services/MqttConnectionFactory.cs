using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.Options;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace Elsa.Mqtt.Services;

internal class MqttConnectionFactory : IMqttConnectionFactory
{
    private readonly MqttOptions _options;
    private readonly IDictionary<string, IMqttConnection> _connections = new Dictionary<string, IMqttConnection>();
    private readonly SemaphoreSlim _semaphore = new(1);

    public MqttConnectionFactory(IOptions<MqttOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<IMqttConnection> CreateConnectionAsync(string? connectionName = null, CancellationToken cancellationToken = default)
    {
        connectionName ??= "Default";

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_connections.TryGetValue(connectionName, out var connection))
            {
                return connection;
            }

            var connectionOptions = ResolveConnection(connectionName);
            var newConnection = new MqttClientFactory().CreateMqttClient();

            var response = await newConnection.ConnectAsync(connectionOptions, cancellationToken);

            var newConnectionProxy = new MqttConnectionProxy(newConnection);

            _connections.Add(connectionName, newConnectionProxy);

            return newConnectionProxy;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resolves a named <see cref="MqttConnectionOptions"/> from the configured options.
    /// </summary>
    public MqttClientOptions ResolveConnection(string? connectionName)
    {
        var name = connectionName ?? "Default";

        if (!_options.Connections.TryGetValue(name, out var connectionOptions))
        {
            throw new InvalidOperationException(
                $"No MQTT connection named '{name}' was found. " +
                $"Configure connections using UseMqtt(options => options.AddConnection(...)).");
        }

        return connectionOptions;
    }
}
