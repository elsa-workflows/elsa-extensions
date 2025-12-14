using Elsa.Studio.Agents.UI.Components;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Agents.Services;

/// <summary>
/// Provides the default implementation for the <see cref="ICreateWorkflowDialogComponentProvider"/> interface.
/// This implementation returns the type of the component used to display the dialog for creating a new workflow.
/// </summary>
public class AICreateWorkflowDialogComponentProvider : ICreateWorkflowDialogComponentProvider
{
    /// <inheritdoc />
    public Type GetComponentType() => typeof(CreateWorkflowDialog);
}