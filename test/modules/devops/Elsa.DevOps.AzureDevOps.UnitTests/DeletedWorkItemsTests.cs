using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class DeletedWorkItemsTests
{
    [Fact]
    public void GetNewlyDeleted_reports_nothing_on_the_first_poll()
    {
        // The recycle bin holds everything ever deleted, so a first poll that reported it would replay years of
        // deletions into live workflows.
        Assert.Empty(DeletedWorkItems.GetNewlyDeleted(null, [1, 2, 3]));
    }

    [Fact]
    public void GetNewlyDeleted_reports_an_id_that_appeared_since_the_previous_poll()
    {
        Assert.Equal([3], DeletedWorkItems.GetNewlyDeleted([1, 2], [1, 2, 3]));
    }

    [Fact]
    public void GetNewlyDeleted_reports_nothing_when_the_recycle_bin_did_not_change()
    {
        Assert.Empty(DeletedWorkItems.GetNewlyDeleted([1, 2], [2, 1]));
    }

    [Fact]
    public void GetNewlyDeleted_ignores_an_id_that_left_the_recycle_bin()
    {
        // Restoring or destroying a work item removes it from the bin; that is not a deletion.
        Assert.Empty(DeletedWorkItems.GetNewlyDeleted([1, 2], [1]));
    }

    [Fact]
    public void GetNewlyDeleted_treats_an_empty_baseline_as_a_real_one()
    {
        // An empty baseline means the bin was empty last time, so anything in it now is new. That is why a poll which
        // never read the bin has to keep its baseline null rather than substituting an empty set.
        Assert.Equal([1, 2], DeletedWorkItems.GetNewlyDeleted([], [1, 2]));
    }

    [Fact]
    public void ToWorkItem_carries_the_fields_the_trigger_filters_on()
    {
        // The Work Item Deleted trigger filters on work item ID and type, and both are read off a WorkItem payload;
        // handing it anything else would silently stop those filters from matching.
        var reference = new WorkItemDeleteReference { Id = 42, Type = "Bug", Name = "Something broke" };

        var workItem = DeletedWorkItems.ToWorkItem(reference, "MyProject");

        Assert.Equal(42, workItem.Id);
        Assert.Equal("Bug", workItem.Fields["System.WorkItemType"]);
        Assert.Equal("Something broke", workItem.Fields["System.Title"]);
        Assert.Equal("MyProject", workItem.Fields["System.TeamProject"]);
    }

    [Fact]
    public void ToWorkItem_leaves_out_a_field_the_recycle_bin_does_not_carry()
    {
        var workItem = DeletedWorkItems.ToWorkItem(new WorkItemDeleteReference { Id = 42 }, "MyProject");

        Assert.False(workItem.Fields.ContainsKey("System.WorkItemType"));
        Assert.Equal(42, workItem.Id);
    }
}
