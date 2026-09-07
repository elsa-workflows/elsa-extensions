using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Watches for file changes in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Triggers when files are created or modified in OneDrive.", Kind = ActivityKind.Trigger)]
public class WatchFiles : Trigger
{
    /// <summary>
    /// The ID of the drive to monitor. If not specified, the user's default drive will be used.
    /// </summary>
    [Input(Description = "The ID of the drive to monitor. If not specified, the user's default drive will be used.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The ID of the folder to monitor. If not specified, the root folder will be monitored.
    /// </summary>
    [Input(Description = "The ID of the folder to monitor. If not specified, the root folder will be monitored.")]
    public Input<string>? FolderId { get; set; }

    /// <summary>
    /// The file extensions to watch for (e.g., '.docx', '.pdf').
    /// </summary>
    [Input(Description = "The file extensions to watch for (e.g., '.docx', '.pdf'). Leave empty to watch all files.")]
    public Input<ICollection<string>>? FileExtensions { get; set; }

    /// <summary>
    /// The polling interval in seconds.
    /// </summary>
    [Input(Description = "The polling interval in seconds.")]
    public Input<int> PollingIntervalInSeconds { get; set; } = new(60);

    /// <summary>
    /// The output file that was created or modified.
    /// </summary>
    [Output(Description = "The file that was created or modified.")]
    public Output<DriveItem> File { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask TriggerAsync(ActivityExecutionContext context)
    {
        var driveId = DriveId?.Get(context);
        var folderId = FolderId?.Get(context);
        var fileExtensions = FileExtensions?.Get(context)?.Select(ext => ext.StartsWith('.') ? ext.ToLowerInvariant() : $".{ext.ToLowerInvariant()}").ToList();
        var pollingIntervalInSeconds = PollingIntervalInSeconds.Get(context);
        var lastPolledTime = DateTimeOffset.UtcNow;
        
        var bookmarkName = GetType().Name;
        var bookmark = context.CreateBookmark(bookmarkName, Resume);
        
        // Schedule a recurring timer to poll for changes
        await context.ScheduleRecurringTimerAsync(
            TimeSpan.FromSeconds(pollingIntervalInSeconds),
            bookmark.Id,
            lastPolledTime);
    }

    private async ValueTask Resume(ActivityExecutionContext context, object? value)
    {
        // Get last polled time from the input or use current time as fallback
        var lastPolledTime = value is DateTimeOffset time ? time : DateTimeOffset.UtcNow.AddHours(-1);
        
        // Get current time for the next polling cycle
        var currentTime = DateTimeOffset.UtcNow;

        // Get the parameters
        var driveId = DriveId?.Get(context);
        var folderId = FolderId?.Get(context);
        var fileExtensions = FileExtensions?.Get(context);
        var pollingIntervalInSeconds = PollingIntervalInSeconds.Get(context);
        
        // Create GraphServiceClient
        var graphClient = context.GetRequiredService<OneDriveClientFactory>().CreateClient();

        // Get the list of files modified since last poll
        DriveItemCollectionResponse result;
        var queryFilter = $"lastModifiedDateTime ge {lastPolledTime.ToString("o")} and file ne null";

        try
        {
            if (driveId != null)
            {
                if (folderId != null)
                {
                    // Get files from specific folder in specific drive
                    result = await graphClient.Drives[driveId].Items[folderId].Children.GetAsync(
                        requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                        cancellationToken: context.CancellationToken);
                }
                else
                {
                    // Get files from root of specific drive
                    result = await graphClient.Drives[driveId].Root.Children.GetAsync(
                        requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                        cancellationToken: context.CancellationToken);
                }
            }
            else if (folderId != null)
            {
                // Get files from specific folder in default drive
                result = await graphClient.Me.Drive.Items[folderId].Children.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                    cancellationToken: context.CancellationToken);
            }
            else
            {
                // Get files from root of default drive
                result = await graphClient.Me.Drive.Root.Children.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                    cancellationToken: context.CancellationToken);
            }

            // Filter results by extension if required
            if (fileExtensions != null && fileExtensions.Any())
            {
                var filteredFiles = result.Value!.Where(file => 
                    file.Name != null && 
                    fileExtensions.Any(ext => file.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
                
                // Trigger workflow for each matching file
                foreach (var file in filteredFiles)
                {
                    File.Set(context, file);
                    
                    // Complete the activity to continue the workflow
                    await context.CompleteActivityAsync();
                }
            }
            else if (result.Value?.Any() == true)
            {
                // Trigger workflow for each file
                foreach (var file in result.Value!)
                {
                    File.Set(context, file);
                    
                    // Complete the activity to continue the workflow
                    await context.CompleteActivityAsync();
                }
            }
        }
        catch (Exception ex)
        {
            context.JournalData.Add("Error", ex.Message);
        }
        
        // Schedule the next poll
        var bookmarkName = GetType().Name;
        var bookmark = context.CreateBookmark(bookmarkName, Resume);
        
        await context.ScheduleRecurringTimerAsync(
            TimeSpan.FromSeconds(pollingIntervalInSeconds),
            bookmark.Id,
            currentTime);
    }
}