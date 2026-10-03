namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// Configuration for the built-in Azure DevOps polling workflows, bound from AzureDevOps:Polling.
/// </summary>
public class AzureDevOpsPollingOptions
{
    /// <summary>
    /// The interval a family falls back to when it configures none of its own, and which itself falls back to
    /// <see cref="DefaultInterval"/> when it is not positive.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The master switch: no poll runs while this is off, whatever the family itself says and whoever asked for the
    /// poll. The only switch a workflow cannot get around, and the only one that also stops what is already running.
    /// </summary>
    public bool Enabled { get; set; }

    public TimeSpan Interval { get; set; } = DefaultInterval;

    /// <summary>
    /// The token every family polls with unless it names one of its own. Prefer <see cref="TokenSecretName"/>, which
    /// keeps the token out of configuration.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The name of the Elsa Secret holding the token every family polls with unless it names one of its own. This is
    /// the place for the polling PAT: it sits below the families and above
    /// <see cref="AzureDevOpsOptions.DefaultTokenSecretName"/>, so a family whose own secret is gone still polls as
    /// polling rather than as the credential activities fall back to.
    /// </summary>
    public string? TokenSecretName { get; set; }

    /// <summary>
    /// What a poll reports about itself. See <see cref="AzureDevOpsPollingDiagnosticsOptions"/>.
    /// </summary>
    public AzureDevOpsPollingDiagnosticsOptions Diagnostics { get; set; } = new();

    public WorkItemPollingOptions WorkItems { get; set; } = new();

    public BuildPollingOptions Builds { get; set; } = new();

    public PullRequestPollingOptions PullRequests { get; set; } = new();

    public PushPollingOptions Pushes { get; set; } = new();

    /// <summary>
    /// Returns how often the given family polls: its own interval, then the shared one, then
    /// <see cref="DefaultInterval"/>. A non-positive value counts as unconfigured, because a timer built from it
    /// would fire without pause.
    /// </summary>
    public TimeSpan ResolveInterval(PollingFamilyOptions family)
    {
        ArgumentNullException.ThrowIfNull(family);

        if (family.Interval is { } familyInterval && familyInterval > TimeSpan.Zero)
            return familyInterval;

        return Interval > TimeSpan.Zero ? Interval : DefaultInterval;
    }
}

/// <summary>
/// The settings every polling family shares. Each family binds its own section under AzureDevOps:Polling, so one
/// family can poll another project, under another token, at another interval.
/// </summary>
public abstract class PollingFamilyOptions
{
    /// <summary>
    /// Whether this family's polling workflow is started when the host starts. That is all it decides: a poll that
    /// something asks for anyway still runs - the manual event on the built-in workflow, or a workflow of your own
    /// carrying the poll activity - and an instance that is already running keeps polling on its timer until it is
    /// stopped. Use <see cref="AzureDevOpsPollingOptions.Enabled"/> to stop polling outright.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often this family polls. Falls back to <see cref="AzureDevOpsPollingOptions.Interval"/> when left empty.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// The organization to poll. Falls back to <see cref="AzureDevOpsOptions.DefaultOrganizationUrl"/> when empty.
    /// </summary>
    public string OrganizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// The project to poll. Falls back to <see cref="AzureDevOpsOptions.DefaultProject"/> when empty.
    /// </summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// The token this family polls with. Falls back to <see cref="TokenSecretName"/>, then to the credential every
    /// family shares (<see cref="AzureDevOpsPollingOptions.Token"/> /
    /// <see cref="AzureDevOpsPollingOptions.TokenSecretName"/>), and only then to
    /// <see cref="AzureDevOpsOptions.DefaultToken"/> / <see cref="AzureDevOpsOptions.DefaultTokenSecretName"/>.
    /// </summary>
    /// <remarks>
    /// A secret named here that holds no value is passed over rather than ending the lookup, so a family whose own
    /// secret was never created falls to the shared polling credential rather than to the default one.
    /// </remarks>
    public string? Token { get; set; }

    /// <summary>
    /// The name of the Elsa Secret holding the token this family polls with. Falls back as <see cref="Token"/>
    /// describes.
    /// </summary>
    public string? TokenSecretName { get; set; }

    public int Top { get; set; } = 100;

    /// <summary>
    /// How far back the first poll looks; later polls continue from their checkpoint.
    /// </summary>
    public TimeSpan LookbackWindow { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Copies this family's configured settings onto <paramref name="target"/> and lays the supplied overrides over
    /// them. Returns <paramref name="target"/>, so a family can add its own members and hand the result on.
    /// </summary>
    /// <remarks>
    /// A copy rather than an edit in place: these options are the host's singleton <c>IOptions</c> value, so writing an
    /// activity's input into them would leak it into every other poll of the host and outlive the instance that set it.
    /// </remarks>
    protected T Overlay<T>(T target, PollingOverrides? overrides)
        where T : PollingFamilyOptions
    {
        ArgumentNullException.ThrowIfNull(target);

        // Neither is overridable; both are copied so the result is a complete stand-in for the configured family.
        target.Enabled = Enabled;
        target.Interval = Interval;

        target.OrganizationUrl = Normalize(overrides?.OrganizationUrl) ?? OrganizationUrl;
        target.Project = Normalize(overrides?.Project) ?? Project;
        target.Top = overrides?.Top is { } top && top > 0 ? top : Top;
        target.LookbackWindow = overrides?.LookbackWindow is { } window && window > TimeSpan.Zero ? window : LookbackWindow;

        // A supplied credential replaces this family's configured pair as a whole. The token resolver prefers a
        // literal token over any secret name, so leaving the configured token in place beside an overriding secret
        // name would quietly keep polling under the configured credential. The rung below the family - the credential
        // every family shares - is untouched by this and still stands in when the supplied one yields nothing.
        if (Normalize(overrides?.Token) is { } suppliedToken)
        {
            target.Token = suppliedToken;
            target.TokenSecretName = null;
        }
        else if (Normalize(overrides?.TokenSecretName) is { } suppliedSecretName)
        {
            target.Token = null;
            target.TokenSecretName = suppliedSecretName;
        }
        else
        {
            target.Token = Token;
            target.TokenSecretName = TokenSecretName;
        }

        return target;
    }

    /// <summary>
    /// Reads a text override the designer may have left empty: whitespace is no value at all, and a value that is
    /// there is trimmed, exactly as the resolvers treat their own inputs.
    /// </summary>
    private protected static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Configuration for work item polling.
/// </summary>
public class WorkItemPollingOptions : PollingFamilyOptions
{
    /// <summary>
    /// Whether each poll also reads the recycle bin to report deleted work items. Switch it off where the polling
    /// token may not read the recycle bin; the other three work item events keep working.
    /// </summary>
    public bool IncludeDeleted { get; set; } = true;

    /// <summary>
    /// Returns what one poll actually runs with: these settings, with the activity's inputs laid over them.
    /// </summary>
    public WorkItemPollingOptions With(WorkItemPollingOverrides? overrides)
    {
        WorkItemPollingOptions target = Overlay(new WorkItemPollingOptions(), overrides);
        target.IncludeDeleted = overrides?.IncludeDeleted ?? IncludeDeleted;

        return target;
    }
}

/// <summary>
/// Configuration for build polling.
/// </summary>
public class BuildPollingOptions : PollingFamilyOptions
{
    /// <summary>
    /// Returns what one poll actually runs with: these settings, with the activity's inputs laid over them.
    /// </summary>
    public BuildPollingOptions With(PollingOverrides? overrides) => Overlay(new BuildPollingOptions(), overrides);
}

/// <summary>
/// Configuration for pull request polling.
/// </summary>
public class PullRequestPollingOptions : PollingFamilyOptions
{
    /// <summary>
    /// Whether a watcher workflow is started per new pull request to report <c>git.pullrequest.updated</c>, which no
    /// query can produce. Switching it off leaves created and merged working.
    /// </summary>
    public bool WatchUpdates { get; set; } = true;

    /// <summary>
    /// How often a watcher re-reads its pull request. Falls back to the interval of this family.
    /// </summary>
    /// <remarks>
    /// Not overridable from the poll activity: the watcher workflow reads this when its definition is built, not when
    /// a poll runs, so an input for it would change nothing.
    /// </remarks>
    public TimeSpan? WatchInterval { get; set; }

    /// <summary>
    /// Returns what one poll actually runs with: these settings, with the activity's inputs laid over them.
    /// </summary>
    public PullRequestPollingOptions With(PullRequestPollingOverrides? overrides)
    {
        PullRequestPollingOptions target = Overlay(new PullRequestPollingOptions(), overrides);
        target.WatchUpdates = overrides?.WatchUpdates ?? WatchUpdates;
        target.WatchInterval = WatchInterval;

        return target;
    }
}

/// <summary>
/// Configuration for push polling.
/// </summary>
public class PushPollingOptions : PollingFamilyOptions
{
    /// <summary>
    /// The repositories to poll, by name or ID. Empty means every repository in the project.
    /// </summary>
    public IList<string> Repositories { get; set; } = [];

    /// <summary>
    /// Returns what one poll actually runs with: these settings, with the activity's inputs laid over them.
    /// </summary>
    public PushPollingOptions With(PushPollingOverrides? overrides)
    {
        PushPollingOptions target = Overlay(new PushPollingOptions(), overrides);
        // Copied rather than shared, so the poll cannot end up writing into the configured list.
        target.Repositories = overrides?.Repositories is { Count: > 0 } supplied ? [.. supplied] : [.. Repositories];

        return target;
    }
}
