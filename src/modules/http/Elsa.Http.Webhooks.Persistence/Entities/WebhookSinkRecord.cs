using Elsa.Common.Entities;

namespace Elsa.Http.Webhooks.Persistence.Entities;

using WebhooksCore;

public class WebhookSinkRecord : Entity
{
    public string Url { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public List<WebhookEventFilter> Filters { get; set; } = new();
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Version { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
