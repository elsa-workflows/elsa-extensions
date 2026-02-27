using Blazored.FluentValidation;
using Elsa.Studio.Http.Webhooks.Client;
using Elsa.Studio.Http.Webhooks.UI.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Http.Webhooks.UI.Components;

public partial class CreateWebhookSinkDialog
{
    private readonly WebhookSinkEditorModel _inputModel = new();
    private EditContext _editContext = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private WebhookSinkInputValidator _validator = null!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    protected override Task OnInitializedAsync()
    {
        _editContext = new EditContext(_inputModel);
        _validator = new WebhookSinkInputValidator();
        return Task.CompletedTask;
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if (!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        MudDialog.Close(_inputModel);
        return Task.CompletedTask;
    }
}
