using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using MassTransit;

namespace Elsa.ServiceBus.MassTransit.Activities;

/// <summary>
/// A generic activity that publishes a message of a given type. Used by the <see cref="MassTransitActivityTypeProvider"/>.
/// </summary>
[Browsable(false)]
public class PublishMessage : CodeActivity
{
    /// <inheritdoc />
    public PublishMessage([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The message type to publish.
    /// </summary>
    public Type MessageType { get; set; } = null!;

    /// <summary>
    /// The message to send. Must be a concrete implementation of the configured <see cref="MessageType"/>.
    /// </summary>
    [Input(
        Description = "The message to send. Must be a concrete implementation of the configured message type.",
        UIHint = InputUIHints.MultiLine
    )]
    public Input<object> Message { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var bus = context.GetRequiredService<IBus>();
        var message = Message.Get(context).ConvertTo(MessageType)!;
        await bus.Publish(message, context.CancellationToken);
    }
}