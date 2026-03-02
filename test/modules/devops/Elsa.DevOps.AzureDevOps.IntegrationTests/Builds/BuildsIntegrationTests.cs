using Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.Builds;

[Collection(AzureDevOpsCollection.Name)]
public class BuildsIntegrationTests(AzureDevOpsFixture fixture)
{
    [SkippableFact]
    public async Task ListBuilds_Returns_Results()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<BuildHttpClient>();

        var builds = await client.GetBuildsAsync(fixture.Project!);

        Assert.NotNull(builds);
    }

    [SkippableFact]
    public async Task ListBuilds_With_Top_Limits_Results()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<BuildHttpClient>();

        var builds = await client.GetBuildsAsync(fixture.Project!, top: 5);

        Assert.NotNull(builds);
        Assert.True(builds.Count <= 5);
    }

    [SkippableFact]
    public async Task GetBuild_For_Known_Build_Returns_Build()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<BuildHttpClient>();
        var builds = await client.GetBuildsAsync(fixture.Project!, top: 1);

        if (builds.Count == 0)
            return;

        var build = await client.GetBuildAsync(fixture.Project!, builds[0].Id);

        Assert.NotNull(build);
        Assert.Equal(builds[0].Id, build.Id);
    }
}
