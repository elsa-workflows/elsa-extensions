using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.Features;

/// <summary>
/// A feature that installs and configures Quartz.NET. Only enable this feature if you are not configuring Quartz.NET yourself.
/// </summary>
public class QuartzFeature : FeatureBase
{
    /// <inheritdoc />
    public QuartzFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that can be used to configure Quartz.NET options.
    /// </summary>
    public Action<QuartzOptions>? ConfigureQuartzOptions { get; set; }

    /// <summary>
    /// A delegate that can be used to configure Quartz.NET itself.
    /// </summary>
    public Action<IServiceCollectionQuartzConfigurator>? ConfigureQuartz { get; set; }

    /// <summary>
    /// A delegate that can be used to configure Quartz.NET hosted service.
    /// </summary>
    public Action<QuartzHostedServiceOptions>? ConfigureQuartzHostedService { get; set; } = options => options.WaitForJobsToComplete = true;

    /// <summary>
    /// Configures the scheduler instance ID and name for clustered operation.
    /// </summary>
    /// <param name="instanceId">The instance ID to use. Use "AUTO" (default) for automatic generation, or specify a unique identifier.</param>
    /// <param name="schedulerName">The scheduler name. Default is "ElsaScheduler".</param>
    /// <remarks>
    /// <para>
    /// This method configures the scheduler instance ID and name, which are essential for clustered operation.
    /// When using "AUTO" for the instance ID, Quartz.NET will generate a unique identifier for each scheduler instance.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> This method only configures the scheduler identity settings. To fully enable clustering, you must also:
    /// 1. Use a persistent job store (e.g., via UseSqlServer, UsePostgreSql, etc.)
    /// 2. Enable clustering on the persistent store (e.g., useClustering=true parameter)
    /// </para>
    /// <para>
    /// Without both a persistent job store and clustering enabled on that store, clustering cannot function properly.
    /// The persistent store provides the shared state required for cluster coordination.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// .UseQuartz(quartz => quartz
    ///     .ConfigureClusteringIdentity()
    ///     .UseSqlServer(connectionString, useClustering: true))
    /// </code>
    /// </para>
    /// </remarks>
    public QuartzFeature ConfigureClusteringIdentity(
        string instanceId = "AUTO", 
        string schedulerName = "ElsaScheduler")
    {
        ConfigureQuartz += quartz =>
        {
            quartz.SchedulerId = instanceId;
            quartz.SchedulerName = schedulerName;
        };

        return this;
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        var type = Type.GetType("Quartz.QuartzHostedService, Quartz.Extensions.Hosting")!;
        Module.ConfigureHostedService(type);

        if (ConfigureQuartzHostedService != null)
            Services.Configure(ConfigureQuartzHostedService);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (ConfigureQuartzOptions != null)
            Services.Configure(ConfigureQuartzOptions);

        Services
            .AddQuartz(configure => { ConfigureQuartzInternal(configure, ConfigureQuartz); });
    }

    private static void ConfigureQuartzInternal(IServiceCollectionQuartzConfigurator quartz, Action<IServiceCollectionQuartzConfigurator>? configureQuartz)
    {
        configureQuartz?.Invoke(quartz);
    }
}