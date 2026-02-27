namespace Elsa.Studio.Http.Webhooks.UI.Pages.Models;

public class WebhookSinkListQueryState
{
    public string? Name { get; set; }
    public bool IncludeDeleted { get; set; }
}

public enum WebhookSinkOperationOutcome
{
    Success,
    ValidationError,
    Conflict,
    Unauthorized,
    TransportError,
    NotFound
}

public record WebhookSinkOperationResult(WebhookSinkOperationOutcome Outcome, string Message, bool RequiresRefresh = false);
