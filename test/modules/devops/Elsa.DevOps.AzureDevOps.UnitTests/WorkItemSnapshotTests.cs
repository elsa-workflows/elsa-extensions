using Elsa.DevOps.AzureDevOps.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class WorkItemSnapshotTests
{
    [Fact]
    public void ReadsTheFieldsAnAgentNeedsToActOn()
    {
        WorkItem workItem = new()
        {
            Id = 42,
            Rev = 7,
            Fields = new Dictionary<string, object>
            {
                ["System.WorkItemType"] = "Bug",
                ["System.State"] = "Active",
                ["System.Title"] = "Kan niet inloggen",
                ["System.Description"] = "<div>Stack trace</div>",
                ["System.AssignedTo"] = new IdentityRef { DisplayName = "Alice Anderson" },
                ["System.Tags"] = "regression; login",
                ["System.ChangedDate"] = new DateTime(2026, 8, 13, 9, 30, 0, DateTimeKind.Utc),
            },
        };

        WorkItemSnapshot snapshot = WorkItemSnapshot.From(workItem, "https://dev.azure.com/contoso");

        Assert.Equal(42, snapshot.Id);
        Assert.Equal("Bug", snapshot.Type);
        Assert.Equal("Active", snapshot.State);
        Assert.Equal("Kan niet inloggen", snapshot.Title);
        Assert.Equal("<div>Stack trace</div>", snapshot.Description);
        // The display name, not the IdentityRef: a model reading "Alice Anderson" can act on it, and the rest of the
        // identity is cost without use.
        Assert.Equal("Alice Anderson", snapshot.AssignedTo);
        Assert.Equal("regression; login", snapshot.Tags);
        Assert.Equal(7, snapshot.Revision);
        // The human URL rather than the API one, because this is what an agent puts in a comment or a reply.
        Assert.Equal("https://dev.azure.com/contoso/_workitems/edit/42", snapshot.Url);
    }

    [Fact]
    public void SurvivesAWorkItemThatHasAlmostNoFields()
    {
        WorkItem workItem = new() { Id = 1, Fields = new Dictionary<string, object>() };

        WorkItemSnapshot snapshot = WorkItemSnapshot.From(workItem, "https://dev.azure.com/contoso/");

        Assert.Equal(1, snapshot.Id);
        Assert.Null(snapshot.Title);
        Assert.Null(snapshot.State);
        Assert.Equal("https://dev.azure.com/contoso/_workitems/edit/1", snapshot.Url);
    }

    [Fact]
    public void ShortensADescriptionThatWouldDominateTheTurn()
    {
        // The conversation trimmer shortens what is stored, not what is sent, so an unbounded description would be paid
        // for in full on the way to the model.
        WorkItem workItem = new()
        {
            Id = 1,
            Fields = new Dictionary<string, object> { ["System.Description"] = new string('x', 9000) },
        };

        WorkItemSnapshot snapshot = WorkItemSnapshot.From(workItem, "https://dev.azure.com/contoso");

        Assert.True(snapshot.Description!.Length < 4200, "The description should have been shortened.");
        Assert.EndsWith("[shortened]", snapshot.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsAnAssignmentThatArrivedAsPlainText()
    {
        // A work item read through a route that does not deserialize identities typed still has to yield a name.
        WorkItem workItem = new()
        {
            Id = 1,
            Fields = new Dictionary<string, object> { ["System.AssignedTo"] = "Alice Anderson" },
        };

        Assert.Equal("Alice Anderson", WorkItemSnapshot.From(workItem, "https://dev.azure.com/x").AssignedTo);
    }
}
