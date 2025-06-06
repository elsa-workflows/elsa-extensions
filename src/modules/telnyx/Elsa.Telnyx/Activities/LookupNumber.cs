﻿using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Returns information about the provided phone number.
/// </summary>
[Activity(Constants.Namespace, "Returns information about the provided phone number.", Kind = ActivityKind.Task)]
public class LookupNumber : CodeActivity<NumberLookupResponse>
{
    /// <inheritdoc />
    public LookupNumber([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The phone number to be looked up.
    /// </summary>
    [Input(Description = "The phone number to be looked up.")]
    public Input<string> PhoneNumber { get; set; } = null!;

    /// <summary>
    /// The types of number lookups to be performed.
    /// </summary>
    [Input(
        Description = "The types of number lookups to be performed.",
        UIHint = InputUIHints.CheckList,
        Options = new[] { "carrier", "caller-name" }
    )]
    public Input<ICollection<string>> Types { get; set; } = new(new List<string>());

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();
        var phoneNumber = PhoneNumber.Get(context) ?? throw new("PhoneNumber is required.");
        var types = Types.Get(context);
        var response = await telnyxClient.NumberLookup.NumberLookupAsync(phoneNumber, types, context.CancellationToken);
        context.Set(Result, response.Data);
    }
}