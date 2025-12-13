using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

public partial class Agent : StudioComponentBase
{
    /// The ID of the agent to edit.
    [Parameter] public string AgentId { get; set; } = null!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool UseJsonResponse
    {
        get => _agent.ExecutionSettings.ResponseFormat == "json_object";
        set => _agent.ExecutionSettings.ResponseFormat = value ? "json_object" : "string";
    }

    private ICollection<ServiceModel> AvailableServices { get; set; } = [];
    private IReadOnlyCollection<string> SelectedServices { get; set; } = [];
    
    private ICollection<SkillDescriptorModel> AvailableSkills { get; set; } = [];
    private IReadOnlyCollection<string> SelectedSkills { get; set; } = [];


    private MudForm _form = null!;
    private AgentInputModelValidator _validator = null!;
    private AgentModel _agent = new();
    private InputVariableConfig? _inputVariableBackup;
    private MudTable<InputVariableConfig> _inputVariableTable;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _validator = new(apiClient);
        var servicesApi = await ApiClientProvider.GetApiAsync<IServicesApi>();
        var skillsApi = await ApiClientProvider.GetApiAsync<ISkillsApi>();
        var servicesResponseList = await servicesApi.ListAsync();
        var skillsResponseList = await skillsApi.ListAsync();
        AvailableServices = servicesResponseList.Items;
        AvailableSkills = skillsResponseList.Items;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _agent = await apiClient.GetAsync(AgentId);
        SelectedServices = _agent.Services.ToList().AsReadOnly();
        SelectedSkills = _agent.Skills.ToList().AsReadOnly();
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        _agent.Services = SelectedServices.ToList();
        _agent.Skills = SelectedSkills.ToList();
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _agent = await apiClient.UpdateAsync(AgentId, _agent);
        Snackbar.Add("Agent successfully updated.", Severity.Success);
        StateHasChanged();
    }

    private void OnAddInputVariableClicked()
    {
        var newInputVariable = new InputVariableConfig
        {
            Name = "Variable1",
            Type = "string"
        };

        _agent.InputVariables.Add(newInputVariable);

        // Need to do it this way, otherwise MudTable doesn't show the item in edit mode.
        _ = Task.Delay(1).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                _inputVariableTable.SetEditingItem(newInputVariable);
                StateHasChanged();
            });
        });
    }

    private void BackupInputVariable(object obj)
    {
        var inputVariable = (InputVariableConfig)obj;
        _inputVariableBackup = new()
        {
            Name = inputVariable.Name,
            Type = inputVariable.Type,
            Description = inputVariable.Description
        };
    }

    private void RestoreInputVariable(object obj)
    {
        var inputVariable = (InputVariableConfig)obj;
        inputVariable.Name = _inputVariableBackup!.Name;
        inputVariable.Type = _inputVariableBackup.Type;
        inputVariable.Description = _inputVariableBackup.Description;
        _inputVariableBackup = null;
    }

    private void CommitInputVariable(object obj)
    {
        _inputVariableBackup = null;
    }

    private Task DeleteInputVariable(InputVariableConfig item)
    {
        _agent.InputVariables.Remove(item);
        StateHasChanged();
        return Task.CompletedTask;
    }
}