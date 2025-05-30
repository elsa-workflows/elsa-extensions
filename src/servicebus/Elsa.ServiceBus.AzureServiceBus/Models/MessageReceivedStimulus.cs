using System.Text.Json.Serialization;

namespace Elsa.ServiceBus.AzureServiceBus.Models;

/// <summary>
/// A bookmark payload model for triggering workflows when messages come in at a given queue or topic and subscription. 
/// </summary>
public record MessageReceivedStimulus
{
    private readonly string _queueOrTopic = null!;
    private readonly string? _subscription;

    /// <summary>
    /// Constructor.
    /// </summary>
    [JsonConstructor]
    public MessageReceivedStimulus()
    {
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public MessageReceivedStimulus(string queueOrTopic, string? subscription)
    {
        QueueOrTopic = queueOrTopic;
        Subscription = subscription;
    }

    /// <summary>
    /// The queue or topic to trigger from.
    /// </summary>
    public string QueueOrTopic
    {
        get => _queueOrTopic;
        init => _queueOrTopic = value.ToLowerInvariant();
    }

    /// <summary>
    /// The subscription to trigger from.
    /// </summary>
    public string? Subscription
    {
        get => _subscription;
        init => _subscription = value?.ToLowerInvariant();
    }
}