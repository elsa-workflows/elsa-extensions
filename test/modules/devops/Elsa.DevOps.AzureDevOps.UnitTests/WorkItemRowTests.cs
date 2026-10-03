using Elsa.DevOps.AzureDevOps.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class WorkItemRowTests
{
    [Fact]
    public void ReadsWhatItTakesToPickAWorkItemOutOfAList()
    {
        WorkItem workItem = new()
        {
            Id = 42,
            Fields = new Dictionary<string, object>
            {
                ["System.WorkItemType"] = "Bug",
                ["System.State"] = "Active",
                ["System.Title"] = "Kan niet inloggen",
                ["System.Tags"] = "regression; login",
                ["System.ChangedDate"] = new DateTime(2026, 8, 13, 9, 30, 0, DateTimeKind.Utc),
                // Asked for by nobody: a row is a line in a list, and descriptions would dominate the result.
                ["System.Description"] = "<div>Stack trace</div>",
            },
        };

        WorkItemRow row = WorkItemRow.From(workItem, "https://dev.azure.com/contoso");

        Assert.Equal(42, row.Id);
        Assert.Equal("Bug", row.Type);
        Assert.Equal("Active", row.State);
        Assert.Equal("Kan niet inloggen", row.Title);
        Assert.Equal("regression; login", row.Tags);
        Assert.Equal(new DateTimeOffset(2026, 8, 13, 9, 30, 0, TimeSpan.Zero), row.ChangedAt);
        Assert.Equal("https://dev.azure.com/contoso/_workitems/edit/42", row.Url);
    }

    [Fact]
    public void AsksAzureDevOpsForOnlyTheFieldsARowShows()
    {
        // Hydrating every field of every hit is the difference between a cheap search and one that fills a turn with text
        // nobody read.
        Assert.Equal(
            ["System.WorkItemType", "System.State", "System.Title", "System.Tags", "System.ChangedDate"],
            WorkItemRow.Fields);
    }

    [Fact]
    public void SurvivesAWorkItemWithNoFieldsAtAll()
    {
        WorkItemRow row = WorkItemRow.From(new WorkItem { Id = 7 }, "https://dev.azure.com/contoso/");

        Assert.Equal(7, row.Id);
        Assert.Null(row.Title);
        Assert.Equal("https://dev.azure.com/contoso/_workitems/edit/7", row.Url);
    }
}
