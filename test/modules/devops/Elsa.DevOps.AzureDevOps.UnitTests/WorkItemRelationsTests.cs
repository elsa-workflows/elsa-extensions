using Elsa.DevOps.AzureDevOps.Activities.WorkItems;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class WorkItemRelationsTests
{
    [Fact]
    public void ToReferenceName_covers_every_relation_type()
    {
        // A relation type added without a mapping would otherwise only surface when a workflow runs.
        foreach (var type in Enum.GetValues<WorkItemLinkType>())
            Assert.False(string.IsNullOrWhiteSpace(WorkItemRelations.ToReferenceName(type)), $"{type} has no reference name.");
    }

    [Fact]
    public void ToReferenceName_gives_every_relation_type_its_own_name()
    {
        var names = Enum.GetValues<WorkItemLinkType>().Select(WorkItemRelations.ToReferenceName).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    // The direction is the part that is easy to get backwards: the enum names the role of the target, while the
    // reference name says forward or reverse, and the two read as opposites for hierarchy and dependency links.
    [InlineData(WorkItemLinkType.Related, "System.LinkTypes.Related")]
    [InlineData(WorkItemLinkType.Parent, "System.LinkTypes.Hierarchy-Reverse")]
    [InlineData(WorkItemLinkType.Child, "System.LinkTypes.Hierarchy-Forward")]
    [InlineData(WorkItemLinkType.Predecessor, "System.LinkTypes.Dependency-Reverse")]
    [InlineData(WorkItemLinkType.Successor, "System.LinkTypes.Dependency-Forward")]
    [InlineData(WorkItemLinkType.Duplicate, "System.LinkTypes.Duplicate-Forward")]
    [InlineData(WorkItemLinkType.DuplicateOf, "System.LinkTypes.Duplicate-Reverse")]
    [InlineData(WorkItemLinkType.TestedBy, "Microsoft.VSTS.Common.TestedBy-Forward")]
    [InlineData(WorkItemLinkType.Tests, "Microsoft.VSTS.Common.TestedBy-Reverse")]
    [InlineData(WorkItemLinkType.Affects, "Microsoft.VSTS.Common.Affects-Forward")]
    [InlineData(WorkItemLinkType.AffectedBy, "Microsoft.VSTS.Common.Affects-Reverse")]
    public void ToReferenceName_maps_the_relation_type_to_the_documented_name(WorkItemLinkType type, string expected)
    {
        Assert.Equal(expected, WorkItemRelations.ToReferenceName(type));
    }

    [Fact]
    public void ToReferenceName_rejects_a_value_that_is_not_a_relation_type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkItemRelations.ToReferenceName((WorkItemLinkType)(-1)));
    }

    [Theory]
    [InlineData("https://dev.azure.com/myorg/_apis/wit/workItems/5678", 5678)]
    // Azure DevOps includes the project GUID on some relations and not on others, which is why the duplicate check
    // compares IDs rather than URL strings.
    [InlineData("https://dev.azure.com/myorg/1f2e3d4c-0000-0000-0000-000000000000/_apis/wit/workItems/5678", 5678)]
    [InlineData("https://dev.azure.com/myorg/_apis/wit/workItems/5678/", 5678)]
    [InlineData("https://dev.azure.com/myorg/_apis/wit/workItems/5678?api-version=7.0", 5678)]
    [InlineData("https://myserver/tfs/DefaultCollection/_apis/wit/workItems/42", 42)]
    public void TryGetWorkItemId_reads_the_id_off_a_work_item_url(string url, int expected)
    {
        Assert.Equal(expected, WorkItemRelations.TryGetWorkItemId(url));
    }

    [Theory]
    [InlineData("vstfs:///Git/Commit/1f2e3d4c%2F9a8b%2Fabcdef")]
    [InlineData("https://example.com/some/document")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryGetWorkItemId_yields_nothing_when_the_url_carries_no_work_item_id(string? url)
    {
        Assert.Null(WorkItemRelations.TryGetWorkItemId(url));
    }

    [Fact]
    public void HasWorkItemRelation_finds_the_same_relation_to_the_same_work_item()
    {
        var workItem = WorkItemWith(("System.LinkTypes.Related", "https://dev.azure.com/myorg/_apis/wit/workItems/5678"));
        Assert.True(WorkItemRelations.HasWorkItemRelation(workItem, "System.LinkTypes.Related", 5678));
    }

    [Fact]
    public void HasWorkItemRelation_ignores_the_same_work_item_under_another_relation()
    {
        // The same two work items may legitimately be linked twice under different types, so a Related link must not
        // hide a Parent link.
        var workItem = WorkItemWith(("System.LinkTypes.Hierarchy-Reverse", "https://dev.azure.com/myorg/_apis/wit/workItems/5678"));
        Assert.False(WorkItemRelations.HasWorkItemRelation(workItem, "System.LinkTypes.Related", 5678));
    }

    [Fact]
    public void HasWorkItemRelation_ignores_another_work_item_under_the_same_relation()
    {
        var workItem = WorkItemWith(("System.LinkTypes.Related", "https://dev.azure.com/myorg/_apis/wit/workItems/1111"));
        Assert.False(WorkItemRelations.HasWorkItemRelation(workItem, "System.LinkTypes.Related", 5678));
    }

    [Fact]
    public void HasWorkItemRelation_ignores_a_hyperlink_whose_url_happens_to_end_in_the_id()
    {
        var workItem = WorkItemWith(("Hyperlink", "https://example.com/5678"));
        Assert.False(WorkItemRelations.HasWorkItemRelation(workItem, "System.LinkTypes.Related", 5678));
    }

    [Fact]
    public void HasWorkItemRelation_handles_a_work_item_without_relations()
    {
        // Azure DevOps leaves Relations null rather than empty when a work item has no links at all.
        Assert.False(WorkItemRelations.HasWorkItemRelation(new WorkItem(), "System.LinkTypes.Related", 5678));
    }

    [Fact]
    public void HasHyperlink_finds_the_same_url()
    {
        var workItem = WorkItemWith(("Hyperlink", "https://example.com/runbook"));
        Assert.True(WorkItemRelations.HasHyperlink(workItem, "https://example.com/runbook"));
    }

    [Theory]
    [InlineData("HTTPS://Example.com/Runbook")]
    [InlineData("  https://example.com/runbook  ")]
    public void HasHyperlink_ignores_case_and_surrounding_whitespace(string url)
    {
        var workItem = WorkItemWith(("Hyperlink", "https://example.com/runbook"));
        Assert.True(WorkItemRelations.HasHyperlink(workItem, url));
    }

    [Fact]
    public void HasHyperlink_ignores_another_url()
    {
        var workItem = WorkItemWith(("Hyperlink", "https://example.com/runbook"));
        Assert.False(WorkItemRelations.HasHyperlink(workItem, "https://example.com/other"));
    }

    [Fact]
    public void HasHyperlink_ignores_a_work_item_relation_with_the_same_url()
    {
        var workItem = WorkItemWith(("System.LinkTypes.Related", "https://example.com/runbook"));
        Assert.False(WorkItemRelations.HasHyperlink(workItem, "https://example.com/runbook"));
    }

    [Fact]
    public void HasHyperlink_handles_a_work_item_without_relations()
    {
        Assert.False(WorkItemRelations.HasHyperlink(new WorkItem(), "https://example.com/runbook"));
    }

    private static WorkItem WorkItemWith(params (string Rel, string Url)[] relations) => new()
    {
        Relations = relations.Select(relation => new WorkItemRelation { Rel = relation.Rel, Url = relation.Url }).ToList()
    };
}
