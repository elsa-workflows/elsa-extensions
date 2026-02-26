using WebhooksCore;

namespace Elsa.Http.Webhooks.Api.Contracts;

public class WebhookSinkInputModel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri Url { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
    public ICollection<WebhookEventFilter> Filters { get; set; } = new List<WebhookEventFilter>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string? ExpectedVersion { get; set; }
}
