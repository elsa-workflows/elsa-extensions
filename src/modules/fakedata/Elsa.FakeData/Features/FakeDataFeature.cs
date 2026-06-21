using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.FakeData.Features;

/// <summary>
/// A feature that provides activities to generate fake data for testing and benchmarking purposes.
/// </summary>
public class FakeDataFeature : FeatureBase
{
    /// <inheritdoc/>
    public FakeDataFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<FakeDataFeature>();
    }
}