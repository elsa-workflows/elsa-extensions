using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Lists available drives in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Lists available drives in OneDrive.", Kind = ActivityKind.Task)]
public class ListDrives : OneDriveActivity<IEnumerable<Drive>>
{
    /// <summary>
    /// The ID of the site to get drives from. If not specified, lists drives from the user's OneDrive.
    /// </summary>
    [Input(Description = "The ID of the site to get drives from. If not specified, lists drives from the user's OneDrive.")]
    public Input<string>? SiteId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var siteId = SiteId?.Get(context);

        DriveCollectionResponse driveResponse;
        if (!string.IsNullOrEmpty(siteId))
        {
            // Get drives for a specific site
            driveResponse = await graphClient.Sites[siteId].Drives.GetAsync(cancellationToken: context.CancellationToken);
        }
        else
        {
            // Get user's drives
            driveResponse = await graphClient.Me.Drives.GetAsync(cancellationToken: context.CancellationToken);
        }

        Result.Set(context, driveResponse.Value ?? new List<Drive>());
    }
}