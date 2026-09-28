namespace Elsa.Bookmarks.Ui.Models;

/// <summary>The well-known kinds a view can carry. A provider may use its own; these are the ones a client can count on.</summary>
public static class BookmarkUiKinds
{
    public const string Link = "link";
    public const string Markdown = "markdown";
    public const string Html = "html";
    public const string Form = "form";
    public const string Choice = "choice";
    public const string Wait = "wait";
}

/// <summary>One open bookmark, with everything a provider needs to recognise and describe it.</summary>
/// <param name="WorkflowInstanceId">The instance the bookmark belongs to.</param>
/// <param name="DefinitionId">The definition that instance runs.</param>
/// <param name="Bookmark">The bookmark itself, flattened to what a provider reads.</param>
/// <param name="InstanceProperties">
/// The instance's own properties. Presentation metadata already lives here - a workflow that set display hints chose
/// better words than a provider can invent - so it travels with the bookmark rather than being looked up again.
/// </param>
public sealed record BookmarkUiContext(
    string WorkflowInstanceId,
    string DefinitionId,
    BookmarkDescriptor Bookmark,
    IReadOnlyDictionary<string, object?> InstanceProperties);

/// <summary>A bookmark, flattened to what a provider reads.</summary>
/// <param name="Id">The bookmark id to resume the instance with.</param>
/// <param name="Name">The name of the bookmark, usually the stimulus the activity waits for.</param>
/// <param name="ActivityId">The id of the waiting activity within the workflow definition.</param>
/// <param name="ActivityNodeId">The node id of the waiting activity.</param>
/// <param name="ActivityInstanceId">The id of the waiting activity's execution context, when the bookmark carries one.</param>
/// <param name="CreatedAt">When the bookmark was created.</param>
/// <param name="Payload">The bookmark's own payload: the stimulus the activity suspended on.</param>
/// <param name="ActivityState">
/// The evaluated inputs of the activity that is waiting. This is where the interesting data lives; the payload is
/// usually no more than an identifier.
/// </param>
public sealed record BookmarkDescriptor(
    string Id,
    string Name,
    string ActivityId,
    string ActivityNodeId,
    string? ActivityInstanceId,
    DateTimeOffset CreatedAt,
    object? Payload,
    IDictionary<string, object>? ActivityState);

/// <summary>What a bookmark should look like to a user, and what has to be collected before it can be answered.</summary>
public sealed record BookmarkUiView
{
    /// <summary>One of <see cref="BookmarkUiKinds"/>, or a provider's own.</summary>
    public required string Kind { get; init; }

    /// <summary>The short label a client shows for this bookmark, for example in a list of open tasks.</summary>
    public required string Title { get; init; }

    /// <summary>The markdown rendering. Always filled: this is what a client without component support shows.</summary>
    public required string Text { get; init; }

    /// <summary>The key a chat resolves against its own component registry, when it has one.</summary>
    public string? ComponentKey { get; init; }

    public IReadOnlyDictionary<string, object?>? ComponentParameters { get; init; }

    /// <summary>What to collect before resuming, or null when this bookmark is not the caller's to answer.</summary>
    public BookmarkResumeSchema? Resume { get; init; }

    /// <summary>When it is worth looking at this instance again. A hint; nothing schedules on it.</summary>
    public DateTimeOffset? RefreshAt { get; init; }

    /// <summary>
    /// Whether this view came from the fallback rather than from a provider that recognised the bookmark. Consumers
    /// use it to decide whether the raw payload is still worth carrying: a described bookmark no longer needs it.
    /// </summary>
    public bool IsFallback { get; init; }
}

/// <summary>What an agent must ask before it can resume, and the fields it has to come back with.</summary>
/// <param name="Prompt">The sentence the agent acts on, for example "Ask whether it can be submitted."</param>
/// <param name="Fields">The fields the agent has to come back with.</param>
public sealed record BookmarkResumeSchema(string Prompt, IReadOnlyList<BookmarkResumeField> Fields);

/// <summary>One field an agent must collect before a bookmark can be resumed.</summary>
/// <param name="Name">The key the answer travels under.</param>
/// <param name="Type">The shape the answer takes.</param>
/// <param name="Label">What a user is shown for this field.</param>
/// <param name="Description">Extra guidance for whoever answers, when the label is not enough.</param>
/// <param name="Required">Whether resuming without this field is refused rather than merely discouraged.</param>
/// <param name="Options">The choices offered when <see cref="Type"/> is <see cref="BookmarkFieldType.Choice"/> or
/// <see cref="BookmarkFieldType.MultiChoice"/>; null otherwise.</param>
/// <param name="DefaultValue">What to use when the agent has nothing better to offer; still subject to <see cref="Required"/>.</param>
public sealed record BookmarkResumeField(
    string Name,
    BookmarkFieldType Type,
    string Label,
    bool Required = false,
    string? Description = null,
    IReadOnlyList<BookmarkFieldOption>? Options = null,
    object? DefaultValue = null);

/// <summary>One choice offered by a <see cref="BookmarkResumeField"/> of type <see cref="BookmarkFieldType.Choice"/>
/// or <see cref="BookmarkFieldType.MultiChoice"/>.</summary>
/// <param name="Value">What is sent back to resume the bookmark.</param>
/// <param name="Label">What a user is shown for this choice.</param>
public sealed record BookmarkFieldOption(string Value, string Label);

/// <summary>
/// The shapes an answer can take. Deliberately data-shaped rather than a copy of any UI's control list: a conversation
/// cannot act on the difference between a password box and a text box, and a provider that needs one says so in
/// <see cref="BookmarkUiView.ComponentParameters"/> instead.
/// </summary>
public enum BookmarkFieldType
{
    Text,
    MultiLineText,
    Number,
    Boolean,
    Date,
    DateTime,
    Choice,
    MultiChoice
}
