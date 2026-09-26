using Elsa.DevOps.AzureDevOps.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class WorkItemTagsTests
{
    [Fact]
    public void AddsATagAfterTheOnesAlreadyThere()
    {
        Assert.Equal("regression; login; triage", WorkItemTags.Add("regression; login", "triage"));
    }

    [Fact]
    public void AddsTheFirstTagToAWorkItemThatHasNone()
    {
        Assert.Equal("triage", WorkItemTags.Add(null, "triage"));
        Assert.Equal("triage", WorkItemTags.Add("   ", "triage"));
    }

    [Fact]
    public void WritesNothingWhenTheTagIsAlreadyThereWhateverItsCasing()
    {
        // Null means "no write needed". Azure DevOps would accept the same value back and spend a revision of the work
        // item's history on nothing.
        Assert.Null(WorkItemTags.Add("Regression; login", "regression"));
    }

    [Fact]
    public void RemovesATagAndKeepsTheRest()
    {
        Assert.Equal("regression; triage", WorkItemTags.Remove("regression; login; triage", "LOGIN"));
    }

    [Fact]
    public void RemovesTheLastTagLeavingTheFieldEmpty()
    {
        // An empty string, not null: null means no write, and clearing the field is a write.
        Assert.Equal(string.Empty, WorkItemTags.Remove("login", "login"));
    }

    [Fact]
    public void WritesNothingWhenTheTagWasNotThere()
    {
        Assert.Null(WorkItemTags.Remove("regression; login", "triage"));
        Assert.Null(WorkItemTags.Remove(null, "triage"));
    }
}
