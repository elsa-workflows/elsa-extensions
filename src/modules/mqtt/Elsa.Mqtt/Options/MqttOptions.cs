using MQTTnet;

namespace Elsa.Mqtt.Options;

/// <summary>
/// Options to configure the MQTT client with.
/// </summary>
public class MqttOptions
{
    /// <summary>
    /// Named MQTT connections. The key is the connection name used in activities.
    /// A connection named <c>Default</c> is used when no connection name is specified.
    /// </summary>
    internal Dictionary<string, MqttClientOptions> Connections { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a named MQTT connection.
    /// </summary>
    public MqttOptions AddConnection(string name, MqttClientOptions options)
    {
        Connections[name] = options;
        return this;
    }

    /// <summary>
    /// Registers a named MQTT connection.
    /// </summary>
    public MqttOptions AddConnection(string name, MqttConnectionOptions options)
    {
        Connections[name] = options.GenerateMqttClientOptions();
        return this;
    }

    /// <summary>
    /// Registers the default MQTT connection (name <c>Default</c>).
    /// </summary>
    public MqttOptions AddDefaultConnection(MqttClientOptions options) => AddConnection("Default", options);

    /// <summary>
    /// Registers the default MQTT connection (name <c>Default</c>).
    /// </summary>
    public MqttOptions AddDefaultConnection(MqttConnectionOptions options) => AddConnection("Default", options.GenerateMqttClientOptions());
}
