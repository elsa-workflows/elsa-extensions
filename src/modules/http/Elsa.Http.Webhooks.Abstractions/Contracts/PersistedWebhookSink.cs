namespace Elsa.Http.Webhooks.Abstractions.Contracts;

using WebhooksCore;

public class PersistedWebhookSink
{
    public string Id { get; set; } = default!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri Url { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public ICollection<WebhookEventFilter> Filters { get; set; } = new List<WebhookEventFilter>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string? Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
