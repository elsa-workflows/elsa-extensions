using Elsa.Studio.Contracts;
using Elsa.Studio.Http.Webhooks.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Http.Webhooks.Tests.Module;

public class WebhookModuleRegistrationTests
{
    [Fact]
    public void AddWebhooksModule_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();

        services.AddWebhooksModule();

        Assert.Contains(services, x => x.ServiceType == typeof(IFeature));
        Assert.Contains(services, x => x.ServiceType == typeof(IMenuProvider));
    }
}
