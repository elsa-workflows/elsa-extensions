using System.Security.Claims;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The target resolver without an activity context: the shape a tool called over MCP arrives in. There is no workflow
/// instance to read a user from, so the caller of the ambient HTTP request is all there is to go on.
/// </summary>
public class WorkItemToolTargetResolverTests
{
    private const string UserToken = "pat-of-alice";
    private const string HostToken = "pat-of-the-host";

    [Fact]
    public async Task ReadsUnderTheCallersOwnTokenWhenThereIsNoActivityContext()
    {
        IAzureDevOpsSecretReader secrets = Substitute.For<IAzureDevOpsSecretReader>();
        secrets.GetSecretAsync("AzureDevOps:alice:Pat", Arg.Any<CancellationToken>()).Returns(UserToken);

        (WorkItemToolTarget? target, string? error) = await Resolver(secrets, SignedInAs("alice@contoso.com"))
            .ResolveAsync(null, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(UserToken, target!.Token);
    }

    [Fact]
    public async Task FallsBackToTheHostTokenWhenTheCallerStoredNone()
    {
        IAzureDevOpsSecretReader secrets = Substitute.For<IAzureDevOpsSecretReader>();
        secrets.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        (WorkItemToolTarget? target, string? error) = await Resolver(secrets, SignedInAs("alice@contoso.com"))
            .ResolveAsync(null, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(HostToken, target!.Token);
    }

    [Fact]
    public async Task FallsBackToTheHostTokenWhenNobodyIsSignedIn()
    {
        IAzureDevOpsSecretReader secrets = Substitute.For<IAzureDevOpsSecretReader>();

        (WorkItemToolTarget? target, string? error) = await Resolver(secrets, httpContext: null)
            .ResolveAsync(null, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(HostToken, target!.Token);
        await secrets.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static HttpContext SignedInAs(string userName) =>
        new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", userName)], "TestAuthentication")),
        };

    private static WorkItemToolTargetResolver Resolver(IAzureDevOpsSecretReader secrets, HttpContext? httpContext)
    {
        AzureDevOpsOptions options = new()
        {
            DefaultOrganizationUrl = "https://dev.azure.com/contoso",
            DefaultProject = "Contoso",
            DefaultToken = HostToken,
        };

        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new WorkItemToolTargetResolver(
            new AzureDevOpsOrganizationUrlResolver(Options.Create(options)),
            new AzureDevOpsProjectResolver(Options.Create(options)),
            new AzureDevOpsTokenResolver(
                Options.Create(options),
                secrets,
                new AzureDevOpsUserNameResolver(accessor),
                NullLogger<AzureDevOpsTokenResolver>.Instance));
    }
}
