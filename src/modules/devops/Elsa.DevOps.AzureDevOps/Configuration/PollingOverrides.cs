namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// What one poll activity says about the settings its family is configured with. Every member is optional: a member
/// left empty keeps the configured value, which is why the built-in polling workflows - which fill in nothing - behave
/// exactly as they did before the activities took inputs at all.
/// </summary>
/// <remarks>
/// Deliberately not a mirror of <see cref="PollingFamilyOptions"/>. <c>Enabled</c> is missing because a poll has
/// nothing to say about it either way: the family switch governs what the host starts, which is settled long before a
/// poll runs, and <see cref="AzureDevOpsPollingOptions.Enabled"/> is the operator's - a workflow definition must not be
/// able to poll a host that has been told not to. <c>Interval</c> is missing because a poll activity does not own its
/// schedule - the timer of the workflow it sits in does.
/// </remarks>
public record PollingOverrides
{
    /// <summary>
    /// The project to poll instead of the configured one.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The organization to poll instead of the configured one.
    /// </summary>
    public string? OrganizationUrl { get; init; }

    /// <summary>
    /// The token to poll with, as a literal personal access token.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// The name of the Elsa Secret holding the token to poll with. Preferred over <see cref="Token"/> for pull
    /// requests: it is the only form of credential a watcher can be handed, because a watcher outlives the poll and
    /// would otherwise have to carry the token itself in its stored instance.
    /// </summary>
    public string? TokenSecretName { get; init; }

    /// <summary>
    /// The page size to read with. A value that is not positive counts as unset, because it would ask Azure DevOps
    /// for nothing at all.
    /// </summary>
    public int? Top { get; init; }

    /// <summary>
    /// How far back the first poll of the instance looks. A value that is not positive counts as unset.
    /// </summary>
    public TimeSpan? LookbackWindow { get; init; }
}

/// <summary>
/// What a work item poll activity says about its family's settings.
/// </summary>
public sealed record WorkItemPollingOverrides : PollingOverrides
{
    public WorkItemPollingOverrides()
    {
    }

    /// <summary>
    /// Takes over the inputs every family shares, leaving this family's own to the caller.
    /// </summary>
    public WorkItemPollingOverrides(PollingOverrides shared)
        : base(shared)
    {
    }

    /// <inheritdoc cref="WorkItemPollingOptions.IncludeDeleted"/>
    public bool? IncludeDeleted { get; init; }
}

/// <summary>
/// What a push poll activity says about its family's settings.
/// </summary>
public sealed record PushPollingOverrides : PollingOverrides
{
    public PushPollingOverrides()
    {
    }

    /// <summary>
    /// Takes over the inputs every family shares, leaving this family's own to the caller.
    /// </summary>
    public PushPollingOverrides(PollingOverrides shared)
        : base(shared)
    {
    }

    /// <summary>
    /// The repositories to poll, by name or ID. An empty collection counts as unset: it is what an input the designer
    /// never filled in looks like, and reading it as "every repository in the project" would widen a poll that only
    /// meant to leave the configured list alone.
    /// </summary>
    public IReadOnlyCollection<string>? Repositories { get; init; }
}

/// <summary>
/// What a pull request poll activity says about its family's settings.
/// </summary>
public sealed record PullRequestPollingOverrides : PollingOverrides
{
    public PullRequestPollingOverrides()
    {
    }

    /// <summary>
    /// Takes over the inputs every family shares, leaving this family's own to the caller.
    /// </summary>
    public PullRequestPollingOverrides(PollingOverrides shared)
        : base(shared)
    {
    }

    /// <inheritdoc cref="PullRequestPollingOptions.WatchUpdates"/>
    public bool? WatchUpdates { get; init; }
}
