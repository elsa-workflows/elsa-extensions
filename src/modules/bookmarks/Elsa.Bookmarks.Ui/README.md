# Elsa.Bookmarks.Ui

Lets each feature say what its own suspended bookmarks should look like to a user, and what has to be collected before
they can be answered.

## Overview

A workflow that suspends says almost nothing about itself. What a caller gets back is a bookmark: an id, a stimulus
name, an opaque payload, and — if it looks in the right place — the evaluated inputs of the activity that is waiting.
That is enough to resume, and nowhere near enough to *ask* anyone anything.

A chat, an agent or any other non-visual client needs three things no bookmark states:

1. **What to show.** A workflow waiting on a webview should produce a link; one waiting on markdown should show the
   markdown; one waiting on a delay should say how long is left.
2. **What to ask.** Before resuming, the caller has to collect something — a confirmation, filled-in form fields, a
   choice per suggestion.
3. **What is even answerable.** A delay must not be "answered"; the caller should report and come back later. A
   trigger is not even a task: a workflow waiting for another system to raise an event has nothing for anyone to
   look at, so it is described as nothing at all.

None of that can live in the client, because the client does not know the features. So each feature registers a
provider for its own bookmarks, and every consumer of this package benefits at once.

The package depends on `Elsa.Workflows.Core`, `Elsa.Workflows.Runtime` and `Elsa.Scheduling`, and deliberately on
nothing else — no MCP, no AI, nothing application-specific.

## Getting started

```bash
dotnet add package Elsa.Bookmarks.Ui
```

Register the mapper and the built-in providers, then your own:

```csharp
services.AddBookmarkUi();                                        // mapper + the generic providers
services.AddBookmarkUiProvider<OrderApprovalBookmarkUiProvider>(); // your feature's provider; calls AddBookmarkUi too
```

Both are idempotent, so every feature that needs the mapper can ask for it without knowing whether another already
did. Register the provider next to the activities it describes.

Then, wherever you have a `WorkflowState` and one of its bookmarks:

```csharp
BookmarkUiContext context = BookmarkActivityStateResolver.CreateContext(state, definitionId, bookmark);
BookmarkUiView? view = await mapper.DescribeAsync(context, cancellationToken);
```

`view.Text` is always filled — that is what a client with no rendering of its own shows.

**`view` is null when there is deliberately nothing to show.** Null is a decision, not a failure: see
[Bookmarks that are shown as nothing](#bookmarks-that-are-shown-as-nothing). Show such a bookmark under its own name,
as you did before this package existed, or leave it out — but do not offer a way to answer it.

## What a view carries

| Member | What it is for |
|---|---|
| `Kind` | `link`, `markdown`, `html`, `form`, `choice`, `wait`, or a provider's own. |
| `Title` | The short label a client shows, for example in a list of open tasks. |
| `Text` | The markdown rendering. Required, because it is the layer that always works. |
| `ComponentKey` | The key a chat resolves against its own component registry, when it has one. |
| `ComponentParameters` | Structure that `Text` cannot express — never a second copy of `Text`. |
| `Resume` | What to collect before resuming, or `null` when this bookmark is not the caller's to answer. |
| `RefreshAt` | When it is worth looking at this instance again. A hint; nothing schedules on it. |
| `IsFallback` | Whether the view came from the fallback rather than from a provider that recognised the bookmark. |

`Resume` is a `BookmarkResumeSchema(Prompt, Fields)`: `Prompt` is the sentence the caller acts on — "Ask whether it can
be submitted." — and `Fields` is what it has to come back with. Field types are deliberately data-shaped
(`Text`, `MultiLineText`, `Number`, `Boolean`, `Date`, `DateTime`, `Choice`, `MultiChoice`) rather than a copy of any
UI's control list: a conversation cannot act on the difference between a password box and a text box, and a provider
that needs one says so in `ComponentParameters` instead.

## Writing a provider

```csharp
public sealed class OrderApprovalBookmarkUiProvider : IBookmarkUiProvider
{
    /// <summary>Lower runs first.</summary>
    public int Order => 50;

    public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default)
    {
        if (!BookmarkPayloadReader.TryRead(context.Bookmark.Payload, out OrderApprovalStimulus? stimulus)
            || string.IsNullOrWhiteSpace(stimulus!.OrderId))
        {
            return ValueTask.FromResult<BookmarkUiView?>(null);   // not mine
        }

        return ValueTask.FromResult<BookmarkUiView?>(new BookmarkUiView
        {
            Kind = BookmarkUiKinds.Form,
            Title = $"Approve order {stimulus.OrderId}",
            Text = $"Order {stimulus.OrderId} is waiting for approval.",
            Resume = new BookmarkResumeSchema(
                "Ask whether the order is approved, and for a reason when it is not.",
                [
                    new BookmarkResumeField("approved", BookmarkFieldType.Boolean, "Approved", Required: true),
                    new BookmarkResumeField("reason", BookmarkFieldType.MultiLineText, "Reason")
                ])
        });
    }
}
```

Four things are worth knowing before you write one.

**Returning `null` *is* "not mine".** There is no separate `CanHandle`; a split would make every provider parse the
same payload twice.

**Guard on a discriminating value, not on "it deserialised".** `System.Text.Json` fills absent members with defaults,
so reading a payload as your own type succeeds on almost any JSON object. Without a value guard — a non-empty id, a
non-default timestamp — your provider claims every other feature's bookmark in the host.

**The interesting data is usually in the activity state, not the payload.** A bookmark's `Payload` is the stimulus;
`BookmarkDescriptor.ActivityState` holds the evaluated inputs of the activity that is waiting, resolved by
`BookmarkActivityStateResolver` from the instance id first and the activity node id second — the second match is what
finds a bookmark created with `includeActivityInstanceId: false`.

**Use `BookmarkPayloadReader`.** A payload is the typed object while the run that created it is still in memory, and a
`JsonElement` once it has been through the instance store. A provider that handles only the first works on a fresh run
and stops working after a restart.

A provider that throws is logged and treated as a decline, and the next one runs: one broken provider must not be able
to fail a read that covers every other bookmark on the instance.

## `IBookmarkResumeWriter`

A schema says *what to ask*. It does not say *how the answers travel* — and for some activities those are not the same
thing. The default is a flat JSON object keyed by the schema's own field names. An activity that reads something else
(an array of answer objects, say) would fault on that, and no answer the caller could send would satisfy both.

The wire format is the waiting activity's business, so a provider that knows better says so:

```csharp
public sealed class OrderApprovalBookmarkUiProvider : IBookmarkUiProvider, IBookmarkResumeWriter
{
    public string WriteAnswers(BookmarkUiContext context, IReadOnlyDictionary<string, object?> values) =>
        JsonSerializer.Serialize(values.Select(entry => new { Name = entry.Key, Value = entry.Value }));
}
```

The consumer validates the caller's answers against the schema, then asks the provider that produced the view to write
the payload. A provider that does not implement the interface gets the flat field map, which is what most activities
want.

## Validating an answer

`BookmarkResumeValidator.Validate(schema, answersJson)` returns either a normalised value per field or a list of
sentences. It checks that required fields are present, that a value converts to its declared type, and that a choice is
one of the offered options; an unknown field is dropped **with a sentence saying so** rather than passed through,
because a workflow that receives a key it never asked for has no way to report the mistake.

Those sentences are what a tool returns to its caller — never an exception. An agent typically runs with detailed
errors off, so an exception reaches its model as something it cannot act on. Surface the non-fatal sentences on the
success path too, or a caller whose field was silently dropped never learns of it.

## The built-in providers

| Provider | Recognises | View |
|---|---|---|
| `DelayBookmarkUiProvider` | `DelayPayload` (`Elsa.Scheduling.Bookmarks`) | `wait`; "Waiting another 7 minutes, until 14:35."; `RefreshAt` = `ResumeAt`; **no** `Resume` |
| `EventBookmarkUiProvider` | `EventStimulus` (`Elsa.Workflows.Runtime.Stimuli`) | `wait` on the event name, resumable with a free payload |
| `RunTaskBookmarkUiProvider` | Elsa's own `RunTask`, by its exact activity type | `form` with a single free-form `answers` field |

One provider covers all four scheduling activities: `DelayPayload` is the only bookmark payload `Elsa.Scheduling`
declares, and `Timer`, `Cron` and `StartAt` all derive from `TimerBase`.

A delay is deliberately **not** resumable. Elsa allows resuming its bookmark early, but a caller that "answers" a delay
has almost always mistaken waiting for asking, and the recovery — a workflow that ran on a schedule it was not supposed
to — is worse than the inconvenience.

`FallbackBookmarkUiProvider` describes whatever is left: `wait`, the bookmark name as the title, one free-form
`answers` field. It offers that field rather than nothing, so an undescribed bookmark stays exactly as answerable as it
was before this package existed — adding descriptions must not take away an ability the caller already had.

It is **not** registered as a provider, and the name is a leftover. It is the mapper's own last resort, because a
fallback taking its turn in the ordered list — even at `int.MaxValue` — claimed every trigger bookmark before the
mapper could decide the case below.

## Bookmarks that are shown as nothing

`DescribeAsync` returns null when the activity the bookmark is waiting in is a **trigger**. There is no view, no
component and nothing to answer.

The reason is that a trigger bookmark is not a task. A workflow suspended in
`Elsa.AzureDevOps.WorkItems.WorkItemDeletedTrigger` is waiting for Azure DevOps to delete a work item; nobody can
"answer" that, and the fallback's free-form field said they could — so every webhook trigger in the host got an input
box that would, if anyone used it, resume the trigger with a payload it cannot read.

The kind is read from the activity registry, keyed on the bookmark's name, which is the activity type name: Elsa names
a bookmark after the activity that created it unless that activity chose a name, and a trigger does not. An activity
the registry does not know is treated as not-a-trigger — a host whose registry was never populated must keep
describing its bookmarks rather than fall silent about all of them at once.

This replaces the fallback and never a provider. `Timer`, `Cron`, `StartAt` and `Event` are triggers too, and the
providers above recognise their bookmarks first, so they keep the views they had.

## Limitations

- Nothing here renders anything. `ComponentKey` and `ComponentParameters` are for a client that has its own component
  registry; a client that ignores them renders `Text`, which is why `Text` is required rather than optional.
- `RefreshAt` is a hint. There is no polling, no held turn and no scheduling; the caller decides when to look again.
- Providers are resolved per call, in `Order`. A tie is resolved by registration order, which is not something to rely
  on — give a specific provider a lower `Order` than the general one it must beat.

## References

- [Elsa Workflows](https://github.com/elsa-workflows/elsa-core)
- `Elsa.Mcp.Server` — the worked consumer: it fills `bookmarks[].ui` and validates a resume against the schema
