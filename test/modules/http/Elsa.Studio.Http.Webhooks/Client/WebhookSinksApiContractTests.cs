using System.Reflection;
using Elsa.Studio.Http.Webhooks.Client;
using Refit;

namespace Elsa.Studio.Http.Webhooks.Tests.Client;

public class WebhookSinksApiContractTests
{
    [Fact]
    public void Interface_ShouldContain_ExpectedEndpointAttributes()
    {
        var type = typeof(IWebhookSinksApi);

        AssertEndpointAttribute<GetAttribute>(type, nameof(IWebhookSinksApi.ListAsync), "/webhook-sinks");
        AssertEndpointAttribute<GetAttribute>(type, nameof(IWebhookSinksApi.GetAsync), "/webhook-sinks/{id}");
        AssertEndpointAttribute<PostAttribute>(type, nameof(IWebhookSinksApi.CreateAsync), "/webhook-sinks");
        AssertEndpointAttribute<PostAttribute>(type, nameof(IWebhookSinksApi.UpdateAsync), "/webhook-sinks/{id}");
        AssertEndpointAttribute<DeleteAttribute>(type, nameof(IWebhookSinksApi.DeleteAsync), "/webhook-sinks/{id}");
        AssertEndpointAttribute<PostAttribute>(type, nameof(IWebhookSinksApi.RestoreAsync), "/webhook-sinks/{id}/restore");
    }

    [Fact]
    public void EditorModelMapper_ShouldMap_ToInputModel()
    {
        var model = new WebhookSinkEditorModel
        {
            Name = "Sink 1",
            TargetUrl = "https://example.com/webhook",
            Description = "Description",
            ExpectedVersion = "v1"
        };

        var input = model.ToInputModel();

        Assert.Equal("Sink 1", input.Name);
        Assert.Equal("https://example.com/webhook", input.Url.ToString());
        Assert.Equal("Description", input.Description);
        Assert.Equal("v1", input.ExpectedVersion);
    }

    private static void AssertEndpointAttribute<TAttribute>(Type type, string methodName, string expectedPath) where TAttribute : Attribute
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        var attribute = method!.GetCustomAttribute<TAttribute>();
        Assert.NotNull(attribute);

        var path = (string?)attribute!.GetType().GetProperty("Path")?.GetValue(attribute);
        Assert.Equal(expectedPath, path);
    }
}
