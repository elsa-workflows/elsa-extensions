using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Sends a sharing invitation for a file or folder in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Sends a sharing invitation for a file or folder in OneDrive.", Kind = ActivityKind.Task)]
public class SendSharingInvitation : OneDriveActivity<Permission>
{
    /// <summary>
    /// The ID or path of the file or folder to share.
    /// </summary>
    [Input(Description = "The ID or path of the file or folder to share.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item to share.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item to share.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The email addresses of the recipients.
    /// </summary>
    [Input(Description = "The email addresses of the recipients.")]
    public Input<IEnumerable<string>> EmailAddresses { get; set; } = default!;

    /// <summary>
    /// The message to include in the invitation.
    /// </summary>
    [Input(Description = "The message to include in the invitation.")]
    public Input<string>? Message { get; set; }

    /// <summary>
    /// The role to grant to the recipients (read, write, etc.).
    /// </summary>
    [Input(Description = "The role to grant to the recipients (read, write, etc.).")]
    public Input<string> Role { get; set; } = new("read");

    /// <summary>
    /// Whether to require signing in to access the shared item.
    /// </summary>
    [Input(Description = "Whether to require signing in to access the shared item.")]
    public Input<bool> RequireSignIn { get; set; } = new(true);

    /// <summary>
    /// Whether to send an email invitation.
    /// </summary>
    [Input(Description = "Whether to send an email invitation.")]
    public Input<bool> SendInvitation { get; set; } = new(true);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var emailAddresses = EmailAddresses.Get(context);
        var message = Message?.Get(context);
        var role = Role.Get(context);
        var requireSignIn = RequireSignIn.Get(context);
        var sendInvitation = SendInvitation.Get(context);
        var driveId = DriveId?.Get(context);

        var recipients = new List<DriveRecipient>();
        foreach (var email in emailAddresses)
        {
            recipients.Add(new DriveRecipient
            {
                Email = email
            });
        }

        var requestBody = new InviteCollectionRequestBody
        {
            Recipients = recipients,
            Message = message,
            RequireSignIn = requireSignIn,
            SendInvitation = sendInvitation,
            Roles = new[] { role }
        };

        Permission permission;
        if (driveId != null)
        {
            // Share with specified drive
            permission = await graphClient.Drives[driveId].Items[itemIdOrPath].Invite.PostAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Share by ID in default drive
            permission = await graphClient.Me.Drive.Items[itemIdOrPath].Invite.PostAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else
        {
            // Share by path in default drive
            permission = await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).Invite.PostAsync(requestBody, cancellationToken: context.CancellationToken);
        }

        Result.Set(context, permission);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}