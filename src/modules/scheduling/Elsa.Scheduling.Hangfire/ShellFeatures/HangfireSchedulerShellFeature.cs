using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Hangfire.ShellFeatures;

/// <summary>
/// Shell feature for Hangfire workflow scheduler.
/// </summary>
[ShellFeature(
    DisplayName = "Hangfire Workflow Scheduler",
    Description = "Uses Hangfire to schedule workflow invocations",
    DependsOn = ["Scheduling"])]
[UsedImplicitly]
public class HangfireSchedulerShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HangfireWorkflowScheduler>();
        services.AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>();
    }
}

