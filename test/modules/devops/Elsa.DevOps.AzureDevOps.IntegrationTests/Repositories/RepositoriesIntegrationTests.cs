using Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.Repositories;

[Collection(AzureDevOpsCollection.Name)]
public class RepositoriesIntegrationTests(AzureDevOpsFixture fixture)
{
    [SkippableFact]
    public async Task GetRepository_Returns_Repository()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();

        var repo = await client.GetRepositoryAsync(fixture.Project!, fixture.Repository!);

        Assert.NotNull(repo);
        Assert.Equal(fixture.Repository, repo.Name);
    }

    [SkippableFact]
    public async Task ListBranches_Returns_At_Least_One_Branch()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();

        var branches = await client.GetBranchesAsync(fixture.Project!, fixture.Repository!);

        Assert.NotNull(branches);
        Assert.NotEmpty(branches);
    }

    [SkippableFact]
    public async Task GetBranch_For_Default_Branch_Returns_Stats()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();
        var branches = await client.GetBranchesAsync(fixture.Project!, fixture.Repository!);

        Assert.NotNull(branches);
        Assert.NotEmpty(branches);

        var first = branches[0];
        var match = branches.FirstOrDefault(b =>
            string.Equals(b.Name, first.Name, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(match);
    }
}
