namespace Elsa.Http.Webhooks.Api.Contracts;

public class WebhookSinkListResponse
{
    public ICollection<WebhookSinkModel> Items { get; set; } = new List<WebhookSinkModel>();
}
