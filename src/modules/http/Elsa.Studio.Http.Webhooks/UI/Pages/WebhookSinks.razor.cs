using Elsa.Http.Webhooks.Api.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Http.Webhooks.Client;
using Elsa.Studio.Http.Webhooks.UI.Components;
using Elsa.Studio.Http.Webhooks.UI.Pages.Models;
using Elsa.Studio.Http.Webhooks.UI.Pages.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Http.Webhooks.UI.Pages;

public partial class WebhookSinks
{
    private MudTable<WebhookSinkModel> _table = null!;
    private readonly WebhookSinkListQueryState _queryState = new();
    private string? _listErrorMessage;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private WebhookSinkOperationResultMapper OperationResultMapper { get; set; } = null!;

    private async Task<IWebhookSinksApi> GetApiAsync()
    {
        return await ApiClientProvider.GetApiAsync<IWebhookSinksApi>();
    }

    private async Task<TableData<WebhookSinkModel>> ServerReload(TableState state, CancellationToken cancellationToken)
    {
        try
        {
            var api = await GetApiAsync();
            var response = await api.ListAsync(new ListWebhookSinksRequest
            {
                Name = _queryState.Name,
                IncludeDeleted = _queryState.IncludeDeleted
            }, cancellationToken);

            _listErrorMessage = null;

            return new TableData<WebhookSinkModel>
            {
                Items = response.Items,
                TotalItems = response.Items.Count
            };
        }
        catch (Exception ex)
        {
            _listErrorMessage = OperationResultMapper.MapException(ex, "Failed to load webhook sinks.").Message;

            return new TableData<WebhookSinkModel>
            {
                Items = Array.Empty<WebhookSinkModel>(),
                TotalItems = 0
            };
        }
    }

    private Task OnNameFilterChanged(string? value)
    {
        _queryState.Name = value;
        Reload();
        return Task.CompletedTask;
    }

    private Task OnIncludeDeletedChanged(bool value)
    {
        _queryState.IncludeDeleted = value;
        Reload();
        return Task.CompletedTask;
    }

    private async Task OnCreateClicked()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };

        var dialog = await DialogService.ShowAsync<CreateWebhookSinkDialog>("Create Webhook Sink", options);
        var result = await dialog.Result;

        if (result is null || result.Canceled || result.Data is not WebhookSinkEditorModel inputModel)
            return;

        try
        {
            var api = await GetApiAsync();
            await api.CreateAsync(inputModel.ToInputModel());
            Snackbar.Add("Webhook sink created.", Severity.Success);
            Reload();
        }
        catch (Exception ex)
        {
            var operationResult = OperationResultMapper.MapException(ex, "Failed to create webhook sink.");
            Snackbar.Add(operationResult.Message, Severity.Error);
        }
    }

    private Task OnEditClicked(string sinkId)
    {
        NavigationManager.NavigateTo($"webhooks/{sinkId}");
        return Task.CompletedTask;
    }

    private async Task OnDeleteClicked(WebhookSinkModel model)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Delete sink",
            $"Delete '{model.Name}'? This operation can be restored.",
            yesText: "Delete",
            noText: "Cancel");

        if (confirm != true)
            return;

        try
        {
            var api = await GetApiAsync();
            await api.DeleteAsync(model.Id, new DeleteWebhookSinkRequest { ExpectedVersion = model.Version });
            Snackbar.Add("Webhook sink deleted.", Severity.Success);
            Reload();
        }
        catch (Exception ex)
        {
            var operationResult = OperationResultMapper.MapException(ex, "Failed to delete webhook sink.");
            Snackbar.Add(operationResult.Message, Severity.Error);
        }
    }

    private async Task OnRestoreClicked(WebhookSinkModel model)
    {
        try
        {
            var api = await GetApiAsync();
            await api.RestoreAsync(model.Id, new RestoreWebhookSinkRequest { ExpectedVersion = model.Version });
            Snackbar.Add("Webhook sink restored.", Severity.Success);
            Reload();
        }
        catch (Exception ex)
        {
            var operationResult = OperationResultMapper.MapException(ex, "Failed to restore webhook sink.");
            Snackbar.Add(operationResult.Message, Severity.Error);
        }
    }

    private void Reload()
    {
        _table.ReloadServerData();
    }
}
