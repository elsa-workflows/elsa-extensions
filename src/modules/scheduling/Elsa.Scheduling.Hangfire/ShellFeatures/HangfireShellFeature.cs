using CShells.Features;
using Hangfire;
using Hangfire.MemoryStorage;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Elsa.Scheduling.Hangfire.ShellFeatures;

/// <summary>
/// Shell feature for Hangfire job scheduler.
/// </summary>
[ShellFeature(
    DisplayName = "Hangfire Scheduler",
    Description = "Enables Hangfire for background job scheduling")]
[UsedImplicitly]
public class HangfireShellFeature : IShellFeature
{
    public bool UseMemoryStorage { get; set; } = true;
    public int WorkerCount { get; set; } = 1;
    public int SchedulePollingIntervalSeconds { get; set; } = 1;

    public void ConfigureServices(IServiceCollection services)
    {
        var jobStorage = UseMemoryStorage ? new() : null ?? new MemoryStorage();

        services.AddHangfire(cfg =>
        {
            cfg.UseSimpleAssemblyNameTypeSerializer();
            cfg.UseRecommendedSerializerSettings(json => json.TypeNameHandling = TypeNameHandling.Objects);
            cfg.UseStorage(jobStorage);
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = WorkerCount;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(SchedulePollingIntervalSeconds);
        });
    }
}

