using MQTTnet;

namespace Elsa.Mqtt.Options;

public class MqttConnectionOptions
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }

    public MqttClientOptions GenerateMqttClientOptions()
    {
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Host, Port)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithClientId(ClientId ?? $"Elsa{Guid.NewGuid():N}");

        if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
        {
            mqttClientOptions.WithCredentials(Username, Password);
        }
        else if (!string.IsNullOrEmpty(Username))
        {
            mqttClientOptions.WithCredentials(Username);
        }

        return mqttClientOptions.Build();
    }
}
