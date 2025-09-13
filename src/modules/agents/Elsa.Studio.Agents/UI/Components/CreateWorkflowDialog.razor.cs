using System.Text.Json;
using System.Text.Json.Nodes;
using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Components;

/// <summary>
/// A dialog that allows the user to create a new workflow.
/// </summary>
public partial class CreateWorkflowDialog
{
    private readonly NewWorkflowModel _metadataModel = new();
    private EditContext _editContext = null!;
    private WorkflowPropertiesModelValidator _validator = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;

    /// <summary>
    /// The name of the workflow to create.
    /// </summary>
    [Parameter] public string WorkflowName { get; set; } = "New workflow";

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _metadataModel.Name = WorkflowName;
        _metadataModel.AIPrompt = string.Empty;
        _editContext = new(_metadataModel);
        _validator = new(WorkflowDefinitionService, Localizer);
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

    private async Task OnValidSubmit()
    {
        var workflowDefinition = await GenerateWorkflowDefinitionFromAIPromptAsync(_metadataModel.AIPrompt);
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        var result = await WorkflowDefinitionService.CreateNewDefinitionAsync(
            _metadataModel.Name!,
            _metadataModel.Description!,
            request =>
            {
                if (workflowDefinition == null)
                    return;

                request.Model.DefinitionId = workflowDefinition["definitionId"]?.ToString() ?? IdentityGenerator.GenerateId();
                request.Model.Name = workflowDefinition["name"]?.ToString() ?? _metadataModel.Name;
                request.Model.Variables = (workflowDefinition["variables"] as JsonArray ?? []).Deserialize<VariableDefinition[]>(serializerOptions);
                request.Model.Inputs = (workflowDefinition["inputs"] as JsonArray ?? []).Deserialize<InputDefinition[]>(serializerOptions);
                request.Model.Outputs = (workflowDefinition["outputs"] as JsonArray ?? []).Deserialize<OutputDefinition[]>(serializerOptions);
                request.Model.Outcomes = (workflowDefinition["outcomes"] as JsonArray ?? []).Deserialize<string[]>(serializerOptions);
                request.Model.ToolVersion = new(workflowDefinition["toolVersion"]?.ToString() ?? "1.0.0");
                request.Model.CustomProperties = workflowDefinition["customProperties"]?.Deserialize<Dictionary<string, object>>(serializerOptions) ?? new Dictionary<string, object>();
                request.Model.Options = workflowDefinition["options"]?.Deserialize<WorkflowOptions>(serializerOptions) ?? new WorkflowOptions();
                request.Model.Root = workflowDefinition["root"] as JsonObject;
            });
        MudDialog.Close(result);
    }

    private async Task<JsonObject?> GenerateWorkflowDefinitionFromAIPromptAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var fileName = prompt switch
        {
            var p when p.Contains("loan", StringComparison.OrdinalIgnoreCase) && p.Contains("email", StringComparison.OrdinalIgnoreCase) => "loan-application-2.json",
            var p when p.Contains("loan", StringComparison.OrdinalIgnoreCase) => "loan-application-1.json",
            var p when p.Contains("oracle", StringComparison.OrdinalIgnoreCase) => "text-document-oracle.json",
            var p when p.Contains("review", StringComparison.OrdinalIgnoreCase) => "document-review-process.json",
            var p when p.Contains("http", StringComparison.OrdinalIgnoreCase) => "hello-world-http.json",
            var p when p.Contains("console", StringComparison.OrdinalIgnoreCase) => "hello-world-console.json",
            var p when p.Contains("writer", StringComparison.OrdinalIgnoreCase) => "content-writer.json",
            var p when p.Contains("pipeline", StringComparison.OrdinalIgnoreCase) => "content-pipeline.json",
            var p when p.Contains("support", StringComparison.OrdinalIgnoreCase) => "customer-support.json",
            var p when p.Contains("customer", StringComparison.OrdinalIgnoreCase) => "customer-support.json",
            _ => null
        };
        
        if (fileName == null)
            return null;
        
        var basePath = AppContext.BaseDirectory;
        var filePath = Path.Combine(basePath, "Assets", fileName);
        await using var stream = File.OpenRead(filePath);
        var json = await JsonNode.ParseAsync(stream);
        return (JsonObject)json!;
    }
}

internal class NewWorkflowModel : WorkflowMetadataModel
{
    public string AIPrompt { get; set; } = null!;
}