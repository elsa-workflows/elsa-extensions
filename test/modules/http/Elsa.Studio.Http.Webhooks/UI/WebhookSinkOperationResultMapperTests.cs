using System.Net;
using Elsa.Studio.Http.Webhooks.UI.Pages.Models;
using Elsa.Studio.Http.Webhooks.UI.Pages.Services;
using Refit;

namespace Elsa.Studio.Http.Webhooks.Tests.UI;

public class WebhookSinkOperationResultMapperTests
{
    [Fact]
    public async Task MapException_ShouldMapConflict()
    {
        var mapper = new WebhookSinkOperationResultMapper();
        var apiException = await CreateApiException(HttpStatusCode.Conflict);

        var result = mapper.MapException(apiException, "default");

        Assert.Equal(WebhookSinkOperationOutcome.Conflict, result.Outcome);
        Assert.True(result.RequiresRefresh);
    }

    [Fact]
    public async Task MapException_ShouldMapNotFound()
    {
        var mapper = new WebhookSinkOperationResultMapper();
        var apiException = await CreateApiException(HttpStatusCode.NotFound);

        var result = mapper.MapException(apiException, "default");

        Assert.Equal(WebhookSinkOperationOutcome.NotFound, result.Outcome);
        Assert.True(result.RequiresRefresh);
    }

    [Fact]
    public async Task MapException_ShouldMapUnauthorized()
    {
        var mapper = new WebhookSinkOperationResultMapper();
        var apiException = await CreateApiException(HttpStatusCode.Forbidden);

        var result = mapper.MapException(apiException, "default");

        Assert.Equal(WebhookSinkOperationOutcome.Unauthorized, result.Outcome);
    }

    [Fact]
    public async Task MapException_ShouldMapValidationError()
    {
        var mapper = new WebhookSinkOperationResultMapper();
        var apiException = await CreateApiException(HttpStatusCode.BadRequest);

        var result = mapper.MapException(apiException, "default");

        Assert.Equal(WebhookSinkOperationOutcome.ValidationError, result.Outcome);
    }

    [Fact]
    public void MapException_ShouldMapTransportError()
    {
        var mapper = new WebhookSinkOperationResultMapper();

        var result = mapper.MapException(new InvalidOperationException("boom"), "default message");

        Assert.Equal(WebhookSinkOperationOutcome.TransportError, result.Outcome);
        Assert.Equal("default message", result.Message);
    }

    private static async Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/webhook-sinks");
        var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent("{}")
        };

        return await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }
}
