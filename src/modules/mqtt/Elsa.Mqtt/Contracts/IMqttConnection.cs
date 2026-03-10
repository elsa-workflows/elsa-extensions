using MQTTnet;
using MQTTnet.Protocol;

namespace Elsa.Mqtt.Contracts;

internal interface IMqttConnection : IDisposable
{
    public Task<MqttClientPublishResult> PublishAsync(
        string topic,
        string message,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
        bool retain = false,
        CancellationToken cancellationToken = default);
}
