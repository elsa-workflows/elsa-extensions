using Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.PullRequests;

[Collection(AzureDevOpsCollection.Name)]
public class PullRequestsIntegrationTests(AzureDevOpsFixture fixture)
{
    [SkippableFact]
    public async Task ListPullRequests_Returns_Results()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();
        var criteria = new GitPullRequestSearchCriteria();

        var pullRequests = await client.GetPullRequestsAsync(
            fixture.Project!, fixture.Repository!, criteria);

        Assert.NotNull(pullRequests);
    }

    [SkippableFact]
    public async Task ListPullRequests_With_Status_Filter_Returns_Results()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();
        var criteria = new GitPullRequestSearchCriteria
        {
            Status = PullRequestStatus.All
        };

        var pullRequests = await client.GetPullRequestsAsync(
            fixture.Project!, fixture.Repository!, criteria, top: 5);

        Assert.NotNull(pullRequests);
        Assert.True(pullRequests.Count <= 5);
    }

    [SkippableFact]
    public async Task GetPullRequest_For_Known_PR_Returns_Details()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<GitHttpClient>();
        var criteria = new GitPullRequestSearchCriteria { Status = PullRequestStatus.All };
        var pullRequests = await client.GetPullRequestsAsync(
            fixture.Project!, fixture.Repository!, criteria, top: 1);

        if (pullRequests.Count == 0)
            return;

        var pr = await client.GetPullRequestByIdAsync(
            fixture.Project!, pullRequests[0].PullRequestId);

        Assert.NotNull(pr);
        Assert.Equal(pullRequests[0].PullRequestId, pr.PullRequestId);
    }
}
