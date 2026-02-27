using System.Net;
using Elsa.Studio.Http.Webhooks.UI.Pages.Models;
using Refit;

namespace Elsa.Studio.Http.Webhooks.UI.Pages.Services;

public class WebhookSinkOperationResultMapper
{
    public WebhookSinkOperationResult MapException(Exception exception, string defaultMessage)
    {
        if (exception is ApiException apiException)
            return MapApiException(apiException, defaultMessage);

        return new WebhookSinkOperationResult(WebhookSinkOperationOutcome.TransportError, defaultMessage);
    }

    private static WebhookSinkOperationResult MapApiException(ApiException apiException, string defaultMessage)
    {
        return apiException.StatusCode switch
        {
            HttpStatusCode.Conflict => new WebhookSinkOperationResult(WebhookSinkOperationOutcome.Conflict, "The sink was changed by another user. Refresh and retry.", true),
            HttpStatusCode.NotFound => new WebhookSinkOperationResult(WebhookSinkOperationOutcome.NotFound, "The sink no longer exists. Refreshing the list is recommended.", true),
            HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => new WebhookSinkOperationResult(WebhookSinkOperationOutcome.Unauthorized, "You do not have permission to perform this action."),
            HttpStatusCode.BadRequest => new WebhookSinkOperationResult(WebhookSinkOperationOutcome.ValidationError, "Please correct validation issues and try again."),
            _ => new WebhookSinkOperationResult(WebhookSinkOperationOutcome.TransportError, defaultMessage)
        };
    }
}
