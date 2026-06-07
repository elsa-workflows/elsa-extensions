using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Gets a sharing link for a file or folder in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Gets a sharing link for a file or folder in OneDrive.", Kind = ActivityKind.Task)]
public class GetShareLink : OneDriveActivity<Permission>
{
    /// <summary>
    /// The ID or path of the file or folder.
    /// </summary>
    [Input(Description = "The ID or path of the file or folder.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The type of sharing link to create.
    /// </summary>
    [Input(Description = "The type of sharing link to create (view, edit, or embed).")]
    public Input<string> LinkType { get; set; } = new("view");

    /// <summary>
    /// The scope of link access (anonymous or organization).
    /// </summary>
    [Input(Description = "The scope of link access (anonymous or organization).")]
    public Input<string> LinkScope { get; set; } = new("anonymous");

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var driveId = DriveId?.Get(context);
        var linkType = LinkType.Get(context);
        var linkScope = LinkScope.Get(context);

        var requestBody = new CreateLinkRequestBody
        {
            Type = linkType,
            Scope = linkScope
        };

        Permission permission;
        try
        {
            if (driveId != null)
            {
                // Create share link with specified drive
                permission = await graphClient.Drives[driveId].Items[itemIdOrPath].CreateLink.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            }
            else if (IsItemId(itemIdOrPath))
            {
                // Create share link by ID in default drive
                permission = await graphClient.Me.Drive.Items[itemIdOrPath].CreateLink.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            }
            else
            {
                // Create share link by path in default drive
                permission = await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).CreateLink.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            }
        }
        catch (ODataError odataError)
        {
            var message = odataError.Error?.Message ?? "Unknown error occurred when creating share link";
            throw new System.InvalidOperationException($"Error creating share link: {message}");
        }

        Result.Set(context, permission);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}