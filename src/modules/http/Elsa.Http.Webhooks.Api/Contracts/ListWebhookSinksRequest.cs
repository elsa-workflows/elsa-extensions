namespace Elsa.Http.Webhooks.Api.Contracts;

public class ListWebhookSinksRequest
{
    public string? Name { get; set; }
    public bool IncludeDeleted { get; set; }
}
