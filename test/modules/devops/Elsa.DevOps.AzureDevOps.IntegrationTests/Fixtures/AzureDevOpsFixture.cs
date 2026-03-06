using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;

/// <summary>
/// Shared fixture that provides a connection to Azure DevOps.
/// Reads configuration from environment variables; tests are skipped at runtime when credentials are absent.
/// <para>
/// Required environment variables:
/// <list type="bullet">
///   <item><c>AZUREDEVOPS_ORG_URL</c> – organization URL (e.g. https://dev.azure.com/myorg)</item>
///   <item><c>AZUREDEVOPS_PAT</c> – personal access token</item>
///   <item><c>AZUREDEVOPS_PROJECT</c> – project name or ID</item>
///   <item><c>AZUREDEVOPS_REPOSITORY</c> – repository name or ID</item>
/// </list>
/// </para>
/// </summary>
public class AzureDevOpsFixture : IDisposable
{
    private const string SkipReason =
        "Azure DevOps integration tests require AZUREDEVOPS_ORG_URL, AZUREDEVOPS_PAT, AZUREDEVOPS_PROJECT, and AZUREDEVOPS_REPOSITORY environment variables.";

    private readonly AzureDevOpsConnectionFactory _factory = new();

    public string? OrganizationUrl { get; } = Environment.GetEnvironmentVariable("AZUREDEVOPS_ORG_URL");
    public string? Pat { get; } = Environment.GetEnvironmentVariable("AZUREDEVOPS_PAT");
    public string? Project { get; } = Environment.GetEnvironmentVariable("AZUREDEVOPS_PROJECT");
    public string? Repository { get; } = Environment.GetEnvironmentVariable("AZUREDEVOPS_REPOSITORY");

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(OrganizationUrl) &&
        !string.IsNullOrWhiteSpace(Pat) &&
        !string.IsNullOrWhiteSpace(Project) &&
        !string.IsNullOrWhiteSpace(Repository);

    /// <summary>
    /// Skips the current test when credentials are not configured.
    /// Uses <c>Xunit.SkippableFact</c>'s <see cref="Skip"/> API — tests must use <c>[SkippableFact]</c>.
    /// </summary>
    public void SkipIfNotConfigured()
    {
        Skip.If(!IsConfigured, SkipReason);
    }

    public VssConnection GetConnection() => _factory.GetConnection(OrganizationUrl!, Pat!);

    public void Dispose() => _factory.Dispose();
}

[CollectionDefinition(Name)]
public class AzureDevOpsCollection : ICollectionFixture<AzureDevOpsFixture>
{
    public const string Name = "AzureDevOps";
}
