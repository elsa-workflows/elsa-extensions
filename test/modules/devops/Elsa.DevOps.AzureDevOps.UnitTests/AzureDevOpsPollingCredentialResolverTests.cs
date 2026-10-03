using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the order in which a poll resolves the personal access token it authenticates with. The rung that matters
/// here is the shared polling credential between the family and the extension-wide default: without it, a family
/// naming a secret that holds no value drops straight to the default PAT - the one activities authenticate with when
/// no user is known, which is the single credential polling must never reach for.
/// </summary>
public class AzureDevOpsPollingCredentialResolverTests
{
    private const string DefaultPatSecret = "AzureDevOps:Pat";
    private const string SharedPollingPatSecret = "AzureDevOps:PollingPat";
    private const string FamilyPollingPatSecret = "AzureDevOps:WorkItems:PollingPat";

    private readonly IAzureDevOpsSecretReader _secretProvider = Substitute.For<IAzureDevOpsSecretReader>();

    [Fact]
    public async Task The_family_secret_wins_over_the_shared_polling_secret()
    {
        Seed(FamilyPollingPatSecret, "family-pat");
        Seed(SharedPollingPatSecret, "shared-pat");
        AzureDevOpsPollingOptions configured = new()
        {
            TokenSecretName = SharedPollingPatSecret,
            WorkItems = { TokenSecretName = FamilyPollingPatSecret },
        };

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.WorkItems, CancellationToken.None);

        Assert.Equal("family-pat", token);
    }

    [Fact]
    public async Task A_family_that_names_no_credential_polls_under_the_shared_polling_secret()
    {
        Seed(SharedPollingPatSecret, "shared-pat");
        AzureDevOpsPollingOptions configured = new() { TokenSecretName = SharedPollingPatSecret };

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.Builds, CancellationToken.None);

        Assert.Equal("shared-pat", token);
    }

    [Fact]
    public async Task A_family_secret_that_holds_no_value_falls_back_to_the_shared_polling_secret_rather_than_the_default()
    {
        // The reported case: the family's own secret was gone, and polling then authenticated as the shared activity
        // PAT without ever looking at the polling PAT beside it.
        Seed(SharedPollingPatSecret, "shared-pat");
        Seed(DefaultPatSecret, "activity-pat");
        AzureDevOpsPollingOptions configured = new()
        {
            TokenSecretName = SharedPollingPatSecret,
            WorkItems = { TokenSecretName = FamilyPollingPatSecret },
        };

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.WorkItems, CancellationToken.None);

        Assert.Equal("shared-pat", token);
    }

    [Fact]
    public async Task A_supplied_secret_that_holds_no_value_falls_back_to_the_shared_polling_secret_rather_than_the_default()
    {
        // A pull request watcher carries the secret name of the poll that started it on its instance, so it outlives
        // the configuration that named it. When that name no longer resolves, the shared polling credential is what it
        // should re-read - not the PAT the activities of this host run as.
        Seed(SharedPollingPatSecret, "shared-pat");
        Seed(DefaultPatSecret, "activity-pat");
        AzureDevOpsPollingOptions configured = new()
        {
            TokenSecretName = SharedPollingPatSecret,
            PullRequests = { TokenSecretName = SharedPollingPatSecret },
        };
        PullRequestPollingOptions pinned = configured.PullRequests.With(new PullRequestPollingOverrides
        {
            TokenSecretName = "AzureDevOps:RemovedPollingPat",
        });

        string? token = await CreateResolver(configured).GetTokenAsync(pinned, CancellationToken.None);

        Assert.Equal("shared-pat", token);
    }

    [Fact]
    public async Task The_shared_polling_token_wins_over_its_own_secret()
    {
        Seed(SharedPollingPatSecret, "shared-pat");
        AzureDevOpsPollingOptions configured = new()
        {
            Token = "inline-shared-pat",
            TokenSecretName = SharedPollingPatSecret,
        };

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.Pushes, CancellationToken.None);

        Assert.Equal("inline-shared-pat", token);
    }

    [Fact]
    public async Task Polling_that_names_no_credential_at_all_still_falls_back_to_the_default_secret()
    {
        // Unchanged behaviour, and the reason the rung above it exists: this is what polling lands on when neither it
        // nor its family names a credential, and it is the activity PAT.
        Seed(DefaultPatSecret, "activity-pat");
        AzureDevOpsPollingOptions configured = new();

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.Builds, CancellationToken.None);

        Assert.Equal("activity-pat", token);
    }

    [Fact]
    public async Task Nothing_configured_anywhere_yields_no_token()
    {
        AzureDevOpsPollingOptions configured = new();

        string? token = await CreateResolver(configured)
            .GetTokenAsync(configured.Builds, CancellationToken.None);

        Assert.Null(token);
    }

    private AzureDevOpsPollingCredentialResolver CreateResolver(AzureDevOpsPollingOptions polling)
    {
        AzureDevOpsTokenResolver tokenResolver = new(
            Options.Create(new AzureDevOpsOptions { DefaultTokenSecretName = DefaultPatSecret }),
            _secretProvider,
            new AzureDevOpsUserNameResolver(Substitute.For<IHttpContextAccessor>()),
            NullLogger<AzureDevOpsTokenResolver>.Instance);

        return new AzureDevOpsPollingCredentialResolver(tokenResolver, Options.Create(polling));
    }

    private void Seed(string secretName, string value) =>
        _secretProvider.GetSecretAsync(secretName, Arg.Any<CancellationToken>()).Returns(value);
}
