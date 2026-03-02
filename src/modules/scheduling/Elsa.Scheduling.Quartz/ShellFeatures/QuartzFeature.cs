using CShells.Features;
using CShells.Hosting;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// Represents a feature that enables Quartz.NET for background job scheduling.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz Scheduler",
    Description = "Enables Quartz.NET for background job scheduling")]
[UsedImplicitly]
public class QuartzFeature : IShellFeature
{
    /// <summary>
    /// Optional delay before the scheduler begins processing jobs after shell activation.
    /// </summary>
    public TimeSpan? StartDelay { get; set; }

    /// <summary>
    /// Whether to wait for running jobs to complete before the scheduler shuts down on
    /// shell deactivation. Defaults to <c>true</c>.
    /// </summary>
    public bool WaitForJobsToComplete { get; set; } = true;

    /// <summary>
    /// The Quartz scheduler instance ID. Use <c>"AUTO"</c> (default) for automatic
    /// generation, which is required for clustering.
    /// </summary>
    public string SchedulerId { get; set; } = "AUTO";

    /// <summary>The Quartz scheduler name. Defaults to <c>"ElsaScheduler"</c>.</summary>
    public string SchedulerName { get; set; } = "ElsaScheduler";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            quartz.UseDefaultThreadPool();
            quartz.SchedulerId = SchedulerId;
            quartz.SchedulerName = SchedulerName;
        });

        // QuartzShellLifecycleHandler owns start/stop — no AddQuartzHostedService needed.
        // The Quartz hosted service is only invoked by the root IHost, which never sees
        // shell-scoped registrations, so registering it would have no effect.
        services.AddSingleton<QuartzShellLifecycleHandler>(sp =>
        {
            return new(
                sp.GetRequiredService<ISchedulerFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QuartzShellLifecycleHandler>>())
            {
                StartDelay = StartDelay,
                WaitForJobsToComplete = WaitForJobsToComplete,
            };
        });

        services.AddSingleton<IShellActivatedHandler>(sp => sp.GetRequiredService<QuartzShellLifecycleHandler>());
        services.AddSingleton<IShellDeactivatingHandler>(sp => sp.GetRequiredService<QuartzShellLifecycleHandler>());
    }
}
