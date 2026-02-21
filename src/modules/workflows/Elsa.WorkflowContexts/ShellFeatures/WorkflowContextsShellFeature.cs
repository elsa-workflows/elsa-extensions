using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.ShellFeatures;

/// <summary>
/// Shell feature for workflow contexts.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Contexts",
    Description = "Provides workflow context management functionality")]
[UsedImplicitly]
public class WorkflowContextsShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWorkflowContextService, DefaultWorkflowContextService>();
    }
}

