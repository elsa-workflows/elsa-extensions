using Elsa.Actors.ProtoActor.Features;
using Elsa.Actors.ProtoActor.HostedServices;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Actors.ProtoActor.UnitTests.Features;

public class ProtoActorFeatureTests
{
    [Fact]
    public void StartClusterMember_IsRegisteredBeforeDependentHostedServices()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.ConfigureHostedService<DependentHostedService>(-1);
        new ProtoActorFeature(module).ConfigureHostedServices();

        module.Apply();

        var hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToList();
        Assert.Collection(
            hostedServices,
            descriptor => Assert.Equal(typeof(StartClusterMember), descriptor.ImplementationType),
            descriptor => Assert.Equal(typeof(DependentHostedService), descriptor.ImplementationType));
    }

    private sealed class DependentHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
