using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Webhooks.Persistence.Features;
using JetBrains.Annotations;

namespace Elsa.Http.Webhooks.Api.Features;

[DependsOn(typeof(WebhookPersistenceFeature))]
[UsedImplicitly]
public class WebhooksApiFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<WebhooksApiFeature>();
    }
}
