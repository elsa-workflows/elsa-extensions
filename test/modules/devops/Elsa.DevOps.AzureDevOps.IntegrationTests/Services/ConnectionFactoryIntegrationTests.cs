using Elsa.DevOps.AzureDevOps.IntegrationTests.Fixtures;
using Microsoft.TeamFoundation.Core.WebApi;

namespace Elsa.DevOps.AzureDevOps.IntegrationTests.Services;

[Collection(AzureDevOpsCollection.Name)]
public class ConnectionFactoryIntegrationTests(AzureDevOpsFixture fixture)
{
    [SkippableFact]
    public async Task Connection_Can_Authenticate_And_List_Projects()
    {
        fixture.SkipIfNotConfigured();

        var connection = fixture.GetConnection();
        var projectClient = connection.GetClient<ProjectHttpClient>();

        var projects = await projectClient.GetProjects();

        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
    }

    [SkippableFact]
    public async Task Connection_Can_Retrieve_Specific_Project()
    {
        fixture.SkipIfNotConfigured();

        var connection = fixture.GetConnection();
        var projectClient = connection.GetClient<ProjectHttpClient>();

        var project = await projectClient.GetProject(fixture.Project!);

        Assert.NotNull(project);
        Assert.Equal(fixture.Project, project.Name);
    }

    [SkippableFact]
    public void Repeated_Calls_Return_Same_Connection()
    {
        fixture.SkipIfNotConfigured();

        var conn1 = fixture.GetConnection();
        var conn2 = fixture.GetConnection();

        Assert.Same(conn1, conn2);
    }
}
