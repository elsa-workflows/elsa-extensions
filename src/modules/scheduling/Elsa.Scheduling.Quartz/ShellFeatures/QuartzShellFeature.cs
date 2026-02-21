using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// Shell feature for Quartz.NET job scheduler.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz Scheduler",
    Description = "Enables Quartz.NET for background job scheduling")]
[UsedImplicitly]
public class QuartzShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQuartz(options =>
        {
            options.UseDefaultThreadPool();
        });

        services.AddQuartzHostedService();
    }
}

