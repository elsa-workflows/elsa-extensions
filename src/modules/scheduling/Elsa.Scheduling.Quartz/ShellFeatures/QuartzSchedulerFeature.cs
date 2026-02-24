using CShells.Features;
using Elsa.Extensions;
using Elsa.Resilience.ShellFeatures;
using Elsa.Scheduling.Quartz.Handlers;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.Tasks;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.ShellFeatures;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// A feature that installs Quartz.NET implementations for <see cref="IWorkflowScheduler"/>.
/// </summary>
[ShellFeature(DependsOn = [
    typeof(QuartzFeature), 
    typeof(SchedulingFeature), 
    typeof(ResilienceFeature)])]
[UsedImplicitly]
public class QuartzSchedulerFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>()
            .AddScoped<IJobKeyProvider, JobKeyProvider>()
            .AddSingleton<IQuartzJobRetryScheduler, QuartzJobRetryScheduler>()
            .AddStartupTask<RegisterJobsTask>()
            .AddScoped<IWorkflowScheduler, QuartzWorkflowScheduler>()
            .AddSingleton<ICronParser, QuartzCronParser>()
            .AddQuartz();
    }
}