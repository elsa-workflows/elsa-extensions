using Elsa.Http.Webhooks.Api.Contracts;

namespace Elsa.Studio.Http.Webhooks.Client;

public class WebhookSinkEditorModel
{
    public string? Name { get; set; }
    public string? TargetUrl { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ExpectedVersion { get; set; }
}

public class DeleteWebhookSinkRequest
{
    public string? ExpectedVersion { get; set; }
}

public static class WebhookSinkApiModelMapper
{
    public static WebhookSinkInputModel ToInputModel(this WebhookSinkEditorModel editorModel)
    {
        return new WebhookSinkInputModel
        {
            Name = editorModel.Name,
            Description = editorModel.Description,
            Url = new Uri(editorModel.TargetUrl!, UriKind.Absolute),
            IsEnabled = editorModel.IsEnabled,
            ExpectedVersion = editorModel.ExpectedVersion
        };
    }

    public static WebhookSinkEditorModel ToEditorModel(this WebhookSinkModel model)
    {
        return new WebhookSinkEditorModel
        {
            Name = model.Name,
            Description = model.Description,
            TargetUrl = model.Url.ToString(),
            IsEnabled = model.IsEnabled,
            ExpectedVersion = model.Version
        };
    }
}
