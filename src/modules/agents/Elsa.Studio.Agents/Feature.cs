using Elsa.Studio.Abstractions;
using Elsa.Studio.Agents.UI.Components.AppBar;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Agents;

[UsedImplicitly]
[RemoteFeature("Elsa.AgentsApi")]
public class Feature(IAppBarService appBarService) : FeatureBase
{
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<Copilot>(100);
        return base.InitializeAsync(cancellationToken);
    }
}