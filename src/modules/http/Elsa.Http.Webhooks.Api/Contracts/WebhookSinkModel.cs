using WebhooksCore;

namespace Elsa.Http.Webhooks.Api.Contracts;

public class WebhookSinkModel
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri Url { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<WebhookEventFilter> Filters { get; set; } = new List<WebhookEventFilter>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string? Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
