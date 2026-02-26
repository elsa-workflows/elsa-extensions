namespace Elsa.Http.Webhooks.Abstractions.Contracts;

public class WebhookSinkFilter
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public bool IncludeDeleted { get; set; }
}
