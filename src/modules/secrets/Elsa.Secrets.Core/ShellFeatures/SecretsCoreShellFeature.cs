using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Core.ShellFeatures;

/// <summary>
/// Shell feature for secrets management core functionality.
/// </summary>
[ShellFeature(
    DisplayName = "Secrets Core",
    Description = "Provides core secrets management functionality for workflows")]
[UsedImplicitly]
public class SecretsCoreShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ISecretProvider, DefaultSecretProvider>();
    }
}

