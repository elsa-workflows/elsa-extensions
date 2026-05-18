using CShells.Features;
using Elsa.PackageManifest.Generator.Hints;
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
    [ManifestSetting(
        DisplayName = "Use memory storage",
        Description = "Use Hangfire's in-memory storage provider.",
        Category = "Storage",
        RestartRequired = true)]
    public bool UseMemoryStorage { get; set; } = true;

    [ManifestSetting(
        DisplayName = "Worker count",
        Description = "The number of Hangfire worker threads to run.",
        Category = "Workers",
        RestartRequired = true)]
    public int WorkerCount { get; set; } = 1;

    [ManifestSetting(
        DisplayName = "Schedule polling interval",
        Description = "The Hangfire schedule polling interval in seconds.",
        Category = "Scheduling",
        RestartRequired = true)]
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
