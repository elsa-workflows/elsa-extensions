using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature for distributed messaging support for workflow management using MassTransit.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Workflow Management",
    Description = "Enables distributed messaging for workflow definition updates using MassTransit",
    DependsOn = ["MassTransit Service Bus", "WorkflowManagement"])]
[UsedImplicitly]
public class MassTransitWorkflowManagementShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddNotificationHandlersFrom(GetType());
    }
}

