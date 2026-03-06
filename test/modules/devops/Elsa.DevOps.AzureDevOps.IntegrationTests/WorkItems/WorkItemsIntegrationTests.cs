using Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.WorkItems;

[Collection(AzureDevOpsCollection.Name)]
public class WorkItemsIntegrationTests(AzureDevOpsFixture fixture)
{
    [SkippableFact]
    public async Task QueryWorkItems_With_Valid_Wiql_Returns_Results()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<WorkItemTrackingHttpClient>();
        var wiql = new Wiql
        {
            Query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{fixture.Project}' ORDER BY [System.Id] DESC"
        };

        var result = await client.QueryByWiqlAsync(wiql, top: 5);

        Assert.NotNull(result);
        Assert.NotNull(result.WorkItems);
    }

    [SkippableFact]
    public async Task CreateWorkItem_And_GetWorkItem_Roundtrips()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<WorkItemTrackingHttpClient>();
        var title = $"Integration Test Work Item – {Guid.NewGuid():N}";
        var description = "Created by Elsa.DevOps.AzureDevOps integration tests. Safe to delete.";
        var document = new JsonPatchDocument
        {
            new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Title",
                Value = title
            },
            new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Description",
                Value = description
            }
        };

        var created = await client.CreateWorkItemAsync(document, fixture.Project!, "Task");

        try
        {
            Assert.NotNull(created);
            Assert.True(created.Id > 0);

            var retrieved = await client.GetWorkItemAsync(created.Id!.Value);

            Assert.NotNull(retrieved);
            Assert.Equal(created.Id, retrieved.Id);
            Assert.Equal(title, retrieved.Fields["System.Title"]);
        }
        finally
        {
            await DeleteWorkItemAsync(client, created.Id!.Value);
        }
    }

    [SkippableFact]
    public async Task UpdateWorkItem_Changes_Title()
    {
        fixture.SkipIfNotConfigured();

        var client = fixture.GetConnection().GetClient<WorkItemTrackingHttpClient>();
        var originalTitle = $"Integration Test – {Guid.NewGuid():N}";
        var createDoc = new JsonPatchDocument
        {
            new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Title",
                Value = originalTitle
            }
        };

        var created = await client.CreateWorkItemAsync(createDoc, fixture.Project!, "Task");

        try
        {
            var updatedTitle = $"Updated – {Guid.NewGuid():N}";
            var updateDoc = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                    Path = "/fields/System.Title",
                    Value = updatedTitle
                }
            };

            var updated = await client.UpdateWorkItemAsync(updateDoc, created.Id!.Value);

            Assert.NotNull(updated);
            Assert.Equal(updatedTitle, updated.Fields["System.Title"]);
        }
        finally
        {
            await DeleteWorkItemAsync(client, created.Id!.Value);
        }
    }

    private static async Task DeleteWorkItemAsync(WorkItemTrackingHttpClient client, int id)
    {
        try
        {
            await client.DeleteWorkItemAsync(id, destroy: true);
        }
        catch
        {
            // Best-effort cleanup; don't fail the test on cleanup errors.
        }
    }
}
