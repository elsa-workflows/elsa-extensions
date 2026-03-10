using Elsa.Mqtt.Contracts;
using MQTTnet;
using MQTTnet.Protocol;

namespace Elsa.Mqtt.Services;

internal class MqttConnectionProxy : IMqttConnection
{
    private readonly IMqttClient _mqttClient;
    private bool _disposed;

    public MqttConnectionProxy(IMqttClient mqttClient)
    {
        _mqttClient = mqttClient;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _mqttClient.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public Task<MqttClientPublishResult> PublishAsync(
        string topic,
        string message,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default)
    {
        return _mqttClient.PublishStringAsync(topic, payload: message, qualityOfServiceLevel, retain, cancellationToken);
    }
}
