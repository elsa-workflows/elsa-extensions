using Blazored.FluentValidation;
using Elsa.Studio.Contracts;
using Elsa.Studio.Http.Webhooks.Client;
using Elsa.Studio.Http.Webhooks.UI.Pages.Services;
using Elsa.Studio.Http.Webhooks.UI.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Http.Webhooks.UI.Pages;

public partial class WebhookSink
{
    [Parameter] public string WebhookSinkId { get; set; } = null!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private WebhookSinkOperationResultMapper OperationResultMapper { get; set; } = null!;

    private WebhookSinkEditorModel _model = new();
    private EditContext _editContext = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private WebhookSinkInputValidator _validator = null!;

    protected override Task OnInitializedAsync()
    {
        _validator = new WebhookSinkInputValidator();
        _editContext = new EditContext(_model);
        return Task.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        var api = await ApiClientProvider.GetApiAsync<IWebhookSinksApi>();
        var sink = await api.GetAsync(WebhookSinkId, includeDeleted: true);
        _model = sink.ToEditorModel();
        _editContext = new EditContext(_model);
    }

    private async Task OnSaveClicked()
    {
        if (!await _fluentValidationValidator.ValidateAsync())
            return;

        try
        {
            var api = await ApiClientProvider.GetApiAsync<IWebhookSinksApi>();
            await api.UpdateAsync(WebhookSinkId, _model.ToInputModel());
            Snackbar.Add("Webhook sink updated.", Severity.Success);
            NavigationManager.NavigateTo("webhooks");
        }
        catch (Exception ex)
        {
            var operationResult = OperationResultMapper.MapException(ex, "Failed to update webhook sink.");
            Snackbar.Add(operationResult.Message, Severity.Error);
        }
    }

    private void OnCancelClicked()
    {
        NavigationManager.NavigateTo("webhooks");
    }
}
