using Elsa.DevOps.AzureDevOps.Services;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the organization URL comparison that decides whether a poll runs against the host's own organization, and
/// the key segment that separates two organizations in a watcher's instance ID.
/// </summary>
public class AzureDevOpsOrganizationUrlTests
{
    [Theory]
    [InlineData("https://dev.azure.com/contoso", "https://dev.azure.com/contoso/")]
    [InlineData("https://dev.azure.com/contoso", "HTTPS://DEV.AZURE.COM/Contoso")]
    [InlineData("https://dev.azure.com/contoso", "  https://dev.azure.com/contoso  ")]
    [InlineData("https://dev.azure.com/contoso", "http://dev.azure.com/contoso")]
    [InlineData(null, "")]
    [InlineData("", "   ")]
    public void AreSame_looks_past_case_scheme_whitespace_and_a_trailing_slash(string? left, string? right) =>
        Assert.True(AzureDevOpsOrganizationUrl.AreSame(left, right));

    [Theory]
    [InlineData("https://dev.azure.com/contoso", "https://dev.azure.com/othercompany")]
    [InlineData("https://dev.azure.com/contoso", "https://contoso.visualstudio.com")]
    [InlineData("https://dev.azure.com/contoso", null)]
    public void AreSame_separates_two_organizations(string? left, string? right) =>
        Assert.False(AzureDevOpsOrganizationUrl.AreSame(left, right));

    [Fact]
    public void ToKeySegment_keeps_the_organization_recognisable()
    {
        // The segment lands in a workflow instance ID, which shows up in Studio, in the logs and in URLs, so it stays
        // readable rather than becoming a hash.
        Assert.Equal("dev.azure.com-contoso", AzureDevOpsOrganizationUrl.ToKeySegment("https://dev.azure.com/contoso"));
    }

    [Fact]
    public void ToKeySegment_keeps_the_host_so_two_hosts_never_collapse()
    {
        // Two organizations of the same name on different hosts are different organizations; dropping the host would
        // give both the same watcher instance ID.
        string dev = AzureDevOpsOrganizationUrl.ToKeySegment("https://dev.azure.com/contoso")!;
        string legacy = AzureDevOpsOrganizationUrl.ToKeySegment("https://contoso.visualstudio.com")!;

        Assert.NotEqual(dev, legacy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToKeySegment_has_nothing_to_say_about_an_empty_url(string? empty) =>
        Assert.Null(AzureDevOpsOrganizationUrl.ToKeySegment(empty));

    [Fact]
    public void ToKeySegment_is_safe_in_an_id()
    {
        // Instance IDs travel in URLs, so anything that would need escaping there becomes a hyphen.
        string? segment = AzureDevOpsOrganizationUrl.ToKeySegment("https://dev.azure.com/care connections/sub");

        Assert.NotNull(segment);
        Assert.DoesNotContain('/', segment);
        Assert.DoesNotContain(' ', segment);
        Assert.DoesNotContain(':', segment);
    }
}
