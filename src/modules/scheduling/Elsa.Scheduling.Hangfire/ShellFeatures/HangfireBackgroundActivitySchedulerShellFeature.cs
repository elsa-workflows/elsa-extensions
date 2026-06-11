using CShells.Features;
using Elsa.Scheduling.Hangfire.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Hangfire.ShellFeatures;

/// <summary>
/// Shell feature for Hangfire background activity scheduler.
/// </summary>
[ShellFeature(
    DisplayName = "Hangfire Background Activity Scheduler",
    Description = "Uses Hangfire to schedule background activities in workflows",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class HangfireBackgroundActivitySchedulerShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HangfireBackgroundActivityScheduler>();
    }
}

