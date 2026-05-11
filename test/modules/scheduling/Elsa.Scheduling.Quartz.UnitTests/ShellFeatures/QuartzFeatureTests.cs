using CShells.Features;
using CShells.Lifecycle;
using Elsa.Scheduling.Quartz.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Quartz.UnitTests.ShellFeatures;

public class QuartzFeatureTests
{
    [Fact]
    public void SchedulerInitializer_IsRegisteredDuringPostConfigure()
    {
        var services = new ServiceCollection();
        var feature = new QuartzFeature();

        feature.ConfigureServices(services);
        services.AddTransient<IShellInitializer, DependentInitializer>();
        ((IPostConfigureShellServices)feature).PostConfigureServices(services);

        var initializerRegistrations = services
            .Where(x => x.ServiceType == typeof(IShellInitializer))
            .ToList();

        Assert.Equal(2, initializerRegistrations.Count);
        Assert.Equal(typeof(DependentInitializer), initializerRegistrations[0].ImplementationType);
        Assert.NotNull(initializerRegistrations[1].ImplementationFactory);
    }

    private sealed class DependentInitializer : IShellInitializer
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
