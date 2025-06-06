﻿using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Transfer a call to a new destination.
/// </summary>
[Activity(Constants.Namespace, "Transfer a call to a new destination.", Kind = ActivityKind.Task)]
[FlowNode("Transferred", "Hangup", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallInitiated, WebhookEventTypes.CallAnswered, WebhookEventTypes.CallHangup)]
public class TransferCall : Activity<CallPayload>
{
    /// <inheritdoc />
    public TransferCall([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(
        Name = "Call Control ID",
        Description = "Unique identifier and token for controlling the call.",
        Category = "Advanced"
    )]
    public Input<string> CallControlId { get; set; } = null!;

    /// <summary>
    /// The DID or SIP URI to dial out and bridge to the given call.
    /// </summary>
    [Input(Description = "The DID or SIP URI to dial out and bridge to the given call.")]
    public Input<string> To { get; set; } = null!;

    /// <summary>
    /// The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.
    /// </summary>
    [Input(Description = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.")]
    public Input<string?> From { get; set; } = null!;

    /// <summary>
    /// The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.
    /// </summary>
    [Input(Description =
        "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field."
    )]
    public Input<string?> FromDisplayName { get; set; } = null!;

    /// <summary>
    /// Enables Answering Machine Detection.
    /// </summary>
    [Input(
        DisplayName = "Answering Machine Detection",
        Description = "Enables Answering Machine Detection.",
        UIHint = InputUIHints.DropDown,
        Options = new[] { "disabled", "detect", "detect_beep", "detect_words", "greeting_end" }
    )]
    public Input<string?> AnsweringMachineDetection { get; set; } = null!;

    /// <summary>
    /// Audio URL to be played back when the transfer destination answers before bridging the call. The URL can point to either a WAV or MP3 file.
    /// </summary>
    [Input(
        DisplayName = "Audio URL",
        Description = "Audio URL to be played back when the transfer destination answers before bridging the call. The URL can point to either a WAV or MP3 file."
    )]
    public Input<Uri?> AudioUrl { get; set; } = null!;

    /// <summary>
    /// Sets the maximum duration of a Call Control Leg in seconds.
    /// </summary>
    [Input(DisplayName = "Time limit", Description = "Sets the maximum duration of a Call Control Leg in seconds.", Category = "Advanced")]
    public Input<int?> TimeLimitSecs { get; set; } = null!;

    /// <summary>
    /// The number of seconds that Telnyx will wait for the call to be answered by the destination to which it is being transferred.
    /// </summary>
    [Input(
        DisplayName = "Timeout",
        Description = "The number of seconds that Telnyx will wait for the call to be answered by the destination to which it is being transferred.",
        Category = "Advanced"
    )]
    public Input<int?> TimeoutSecs { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await TransferCallAsync(context);

        var initiatedBookmark = new WebhookEventStimulus(WebhookEventTypes.CallInitiated);
        context.CreateBookmark(initiatedBookmark, InitiatedAsync);
    }

    private ValueTask InitiatedAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallInitiatedPayload>();
        var callControlId = payload.CallControlId;
        var answeredBookmark = new WebhookEventStimulus(WebhookEventTypes.CallAnswered, callControlId);
        var hangupBookmark = new WebhookEventStimulus(WebhookEventTypes.CallHangup, callControlId);
        context.CreateBookmark(answeredBookmark, AnsweredAsync, false);
        context.CreateBookmark(hangupBookmark, HangupAsync, false);
        return default;
    }

    private async ValueTask AnsweredAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallAnsweredPayload>();
        Result.Set(context, payload);
        await context.CompleteActivityWithOutcomesAsync("Transferred");
    }

    private async ValueTask HangupAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallHangupPayload>();
        Result.Set(context, payload);
        await context.CompleteActivityWithOutcomesAsync("Hangup");
    }

    private async ValueTask TransferCallAsync(ActivityExecutionContext context)
    {
        var callControlId = CallControlId.Get(context);

        var request = new TransferCallRequest(
            To.Get(context) ?? throw new("To is required."),
            From.GetOrDefault(context),
            FromDisplayName.GetOrDefault(context).SanitizeCallerName(),
            AudioUrl.GetOrDefault(context),
            AnsweringMachineDetection.GetOrDefault(context),
            null,
            TimeLimitSecs.GetOrDefault(context),
            TimeoutSecs.GetOrDefault(context),
            ClientState: context.CreateCorrelatingClientState(context.Id),
            TargetLegClientState: context.CreateCorrelatingClientState(context.Id)
        );


        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.TransferCallAsync(callControlId, request, context.CancellationToken);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync(context.CancellationToken)) throw;
            await context.CompleteActivityWithOutcomesAsync("Disconnected");
        }
    }
}