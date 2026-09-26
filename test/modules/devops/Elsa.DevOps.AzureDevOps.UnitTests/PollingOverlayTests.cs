using Elsa.DevOps.AzureDevOps.Configuration;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the overlay a poll activity's inputs form over the configured family settings. Two properties matter beyond
/// the plain "an input wins" case: an input left empty in the designer must keep the configured value rather than
/// blank it, and the overlay must never write into the configured options themselves, which are the singleton
/// <c>IOptions</c> instance every other poll of the host shares.
/// </summary>
public class PollingOverlayTests
{
    [Fact]
    public void With_keeps_every_configured_value_when_nothing_is_overridden()
    {
        // The built-in polling workflows leave every input empty, so this is the path that has to behave exactly as
        // the poller did before inputs existed.
        BuildPollingOptions configured = new()
        {
            OrganizationUrl = "https://dev.azure.com/contoso",
            Project = "Contoso",
            TokenSecretName = "AzureDevOps:PollingPat",
            Top = 42,
            LookbackWindow = TimeSpan.FromMinutes(30),
        };

        BuildPollingOptions effective = configured.With(new PollingOverrides());

        Assert.Equal("https://dev.azure.com/contoso", effective.OrganizationUrl);
        Assert.Equal("Contoso", effective.Project);
        Assert.Equal("AzureDevOps:PollingPat", effective.TokenSecretName);
        Assert.Equal(42, effective.Top);
        Assert.Equal(TimeSpan.FromMinutes(30), effective.LookbackWindow);
    }

    [Fact]
    public void With_leaves_the_configured_options_untouched()
    {
        // The configured options are the host's singleton IOptions value. Writing an activity's input into them would
        // leak that input into every other poll of every other workflow, and outlive the instance that set it.
        BuildPollingOptions configured = new() { Project = "Contoso", Top = 100 };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Project = "Contoso", Top = 5 });

        Assert.Equal("Contoso", configured.Project);
        Assert.Equal(100, configured.Top);
        Assert.NotSame(configured, effective);
        Assert.Equal("Contoso", effective.Project);
        Assert.Equal(5, effective.Top);
    }

    [Fact]
    public void With_takes_the_supplied_organization_project_and_volume()
    {
        BuildPollingOptions configured = new()
        {
            OrganizationUrl = "https://dev.azure.com/contoso",
            Project = "Contoso",
            Top = 100,
            LookbackWindow = TimeSpan.FromMinutes(10),
        };

        BuildPollingOptions effective = configured.With(new PollingOverrides
        {
            OrganizationUrl = "https://dev.azure.com/other",
            Project = "Contoso Web Platform",
            Top = 25,
            LookbackWindow = TimeSpan.FromHours(2),
        });

        Assert.Equal("https://dev.azure.com/other", effective.OrganizationUrl);
        Assert.Equal("Contoso Web Platform", effective.Project);
        Assert.Equal(25, effective.Top);
        Assert.Equal(TimeSpan.FromHours(2), effective.LookbackWindow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void With_reads_a_blank_text_override_as_unset(string? blank)
    {
        // An input the designer never filled in arrives as null or empty. Reading it as "clear the configured value"
        // would leave the poll without a project and silently do nothing.
        BuildPollingOptions configured = new() { Project = "Contoso", OrganizationUrl = "https://dev.azure.com/contoso" };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Project = blank, OrganizationUrl = blank });

        Assert.Equal("Contoso", effective.Project);
        Assert.Equal("https://dev.azure.com/contoso", effective.OrganizationUrl);
    }

    [Fact]
    public void With_trims_a_supplied_text_override()
    {
        BuildPollingOptions configured = new() { Project = "Contoso" };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Project = "  Contoso Web Platform  " });

        Assert.Equal("Contoso Web Platform", effective.Project);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void With_reads_a_non_positive_top_as_unset(int top)
    {
        // A page size of zero or less asks Azure DevOps for nothing at all, so it counts as unconfigured the same way
        // a non-positive interval does.
        BuildPollingOptions configured = new() { Top = 100 };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Top = top });

        Assert.Equal(100, effective.Top);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void With_reads_a_non_positive_lookback_window_as_unset(int minutes)
    {
        BuildPollingOptions configured = new() { LookbackWindow = TimeSpan.FromMinutes(10) };

        BuildPollingOptions effective = configured.With(new PollingOverrides { LookbackWindow = TimeSpan.FromMinutes(minutes) });

        Assert.Equal(TimeSpan.FromMinutes(10), effective.LookbackWindow);
    }

    [Fact]
    public void With_replaces_the_whole_credential_when_a_secret_name_is_supplied()
    {
        // The token resolver prefers a literal token over any secret name, so leaving the configured token in place
        // beside an overriding secret name would silently keep polling under the configured credential.
        BuildPollingOptions configured = new() { Token = "configured-pat", TokenSecretName = "AzureDevOps:PollingPat" };

        BuildPollingOptions effective = configured.With(new PollingOverrides { TokenSecretName = "AzureDevOps:ContosoPollingPat" });

        Assert.Null(effective.Token);
        Assert.Equal("AzureDevOps:ContosoPollingPat", effective.TokenSecretName);
    }

    [Fact]
    public void With_replaces_the_whole_credential_when_a_token_is_supplied()
    {
        BuildPollingOptions configured = new() { Token = "configured-pat", TokenSecretName = "AzureDevOps:PollingPat" };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Token = "supplied-pat" });

        Assert.Equal("supplied-pat", effective.Token);
        Assert.Null(effective.TokenSecretName);
    }

    [Fact]
    public void With_keeps_the_configured_credential_when_neither_is_supplied()
    {
        BuildPollingOptions configured = new() { Token = "configured-pat", TokenSecretName = "AzureDevOps:PollingPat" };

        BuildPollingOptions effective = configured.With(new PollingOverrides());

        Assert.Equal("configured-pat", effective.Token);
        Assert.Equal("AzureDevOps:PollingPat", effective.TokenSecretName);
    }

    [Fact]
    public void With_keeps_the_switches_and_the_interval_out_of_reach()
    {
        // Enabled is the operator's switch and the interval belongs to the workflow's timer, not to one poll; neither
        // is something a workflow definition may talk itself into.
        BuildPollingOptions configured = new() { Enabled = false, Interval = TimeSpan.FromMinutes(3) };

        BuildPollingOptions effective = configured.With(new PollingOverrides { Project = "Contoso" });

        Assert.False(effective.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(3), effective.Interval);
    }

    [Fact]
    public void With_takes_the_supplied_include_deleted()
    {
        WorkItemPollingOptions configured = new() { IncludeDeleted = true };

        WorkItemPollingOptions effective = configured.With(new WorkItemPollingOverrides { IncludeDeleted = false });

        Assert.False(effective.IncludeDeleted);
    }

    [Fact]
    public void With_keeps_the_configured_include_deleted_when_the_input_is_empty()
    {
        WorkItemPollingOptions configured = new() { IncludeDeleted = false };

        WorkItemPollingOptions effective = configured.With(new WorkItemPollingOverrides());

        Assert.False(effective.IncludeDeleted);
    }

    [Fact]
    public void With_takes_the_supplied_repositories()
    {
        PushPollingOptions configured = new() { Repositories = ["Studio"] };

        PushPollingOptions effective = configured.With(new PushPollingOverrides { Repositories = ["Contoso", "Contoso.Api"] });

        Assert.Equal(["Contoso", "Contoso.Api"], effective.Repositories);
        Assert.Equal(["Studio"], configured.Repositories);
    }

    [Fact]
    public void With_reads_an_empty_repository_list_as_unset()
    {
        // An input the designer never filled in arrives as an empty collection, which is indistinguishable from "no
        // repositories". Reading it as "every repository in the project" would widen the poll of a workflow that only
        // meant to leave the input alone.
        PushPollingOptions configured = new() { Repositories = ["Studio"] };

        PushPollingOptions effective = configured.With(new PushPollingOverrides { Repositories = [] });

        Assert.Equal(["Studio"], effective.Repositories);
    }

    [Fact]
    public void With_copies_the_configured_repositories_rather_than_sharing_them()
    {
        PushPollingOptions configured = new() { Repositories = ["Studio"] };

        PushPollingOptions effective = configured.With(new PushPollingOverrides());
        effective.Repositories.Add("Contoso");

        Assert.Equal(["Studio"], configured.Repositories);
    }

    [Fact]
    public void With_takes_the_supplied_watch_updates()
    {
        PullRequestPollingOptions configured = new() { WatchUpdates = true, WatchInterval = TimeSpan.FromMinutes(2) };

        PullRequestPollingOptions effective = configured.With(new PullRequestPollingOverrides { WatchUpdates = false });

        Assert.False(effective.WatchUpdates);
        // The watcher workflow reads its interval from configuration when its definition is built, so this one is not
        // an input: carrying it here would suggest an activity could change it.
        Assert.Equal(TimeSpan.FromMinutes(2), effective.WatchInterval);
    }
}
