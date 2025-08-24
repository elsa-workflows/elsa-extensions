using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Persistence.EFCore;

/// <summary>
/// Configures the EF Core persistence providers for the secret management feature.
/// </summary>
[DependsOn(typeof(TestingFeature))]
public class EFCoreTestingPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreTestingPersistenceFeature, TestingDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<TestingFeature>(feature =>
        {
            feature
                .UseTestScenarioStore(sp => sp.GetRequiredService<EFCoreTestScenarioStore>())
                .UseTestRunStore(sp => sp.GetRequiredService<EFCoreTestRunStore>())
                .UseTestSuiteStore(sp => sp.GetRequiredService<EFCoreTestSuiteStore>())
                .UseTestSuiteRunStore(sp => sp.GetRequiredService<EFCoreTestSuiteRunStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<TestScenario, EFCoreTestScenarioStore>();
    }
}