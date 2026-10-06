using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.RuntimeWorkflows;
using Elsa.DevOps.AzureDevOps.Services;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class AzureDevOpsPollingWorkflowStarterTests
{
    [Fact]
    public void GetEnabledDefinitionIds_starts_nothing_while_polling_is_off()
    {
        // The master switch has to win, or switching polling off would still leave the timers running.
        var options = new AzureDevOpsPollingOptions { Enabled = false };

        Assert.Empty(AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }

    [Fact]
    public void GetEnabledDefinitionIds_starts_work_item_polling_when_its_family_is_on()
    {
        var options = new AzureDevOpsPollingOptions { Enabled = true };

        Assert.Contains(AzureDevOpsPollingWorkflowNames.WorkItems, AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }

    [Fact]
    public void GetEnabledDefinitionIds_skips_a_family_that_is_switched_off()
    {
        var options = new AzureDevOpsPollingOptions { Enabled = true };
        options.WorkItems.Enabled = false;

        Assert.DoesNotContain(AzureDevOpsPollingWorkflowNames.WorkItems, AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }

    [Fact]
    public void The_work_item_definition_id_keeps_the_value_existing_instances_were_created_under()
    {
        // Changing this orphans the singleton instance of every deployment that already polls.
        Assert.Equal("AzureDevOpsWorkItemPollingWorkflow", AzureDevOpsPollingWorkflowNames.WorkItems);
    }

    [Fact]
    public void GetEnabledDefinitionIds_starts_build_polling_when_its_family_is_on()
    {
        var options = new AzureDevOpsPollingOptions { Enabled = true };

        Assert.Contains(AzureDevOpsPollingWorkflowNames.Builds, AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }

    [Fact]
    public void GetEnabledDefinitionIds_starts_pull_request_polling_when_its_family_is_on()
    {
        var options = new AzureDevOpsPollingOptions { Enabled = true };

        Assert.Contains(AzureDevOpsPollingWorkflowNames.PullRequests, AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }

    [Fact]
    public void GetEnabledDefinitionIds_starts_push_polling_when_its_family_is_on()
    {
        var options = new AzureDevOpsPollingOptions { Enabled = true };

        Assert.Contains(AzureDevOpsPollingWorkflowNames.Pushes, AzureDevOpsPollingWorkflowStarter.GetEnabledDefinitionIds(options));
    }
}
