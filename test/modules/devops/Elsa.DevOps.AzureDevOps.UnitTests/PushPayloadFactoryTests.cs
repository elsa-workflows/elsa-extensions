using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class PushPayloadFactoryTests
{
    private static GitPush Push() => new()
    {
        PushId = 42,
        Date = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc),
        Repository = new GitRepository { Id = Guid.NewGuid(), Name = "MyRepo" },
        RefUpdates = [new GitRefUpdate { Name = "refs/heads/main", NewObjectId = "abc123" }],
    };

    [Fact]
    public void ToPayload_names_properties_the_way_a_service_hook_does()
    {
        // The Code Pushed trigger hands the payload out as raw JSON, so a workflow reading repository.name has to
        // work whether the event arrived over a webhook or over polling.
        var payload = PushPayloadFactory.ToPayload(Push());

        Assert.Equal("MyRepo", payload.GetProperty("repository").GetProperty("name").GetString());
        Assert.Equal(42, payload.GetProperty("pushId").GetInt32());
    }

    [Fact]
    public void ToPayload_keeps_the_ref_updates()
    {
        var payload = PushPayloadFactory.ToPayload(Push());

        var refUpdate = Assert.Single(payload.GetProperty("refUpdates").EnumerateArray().ToList());
        Assert.Equal("refs/heads/main", refUpdate.GetProperty("name").GetString());
    }

    [Fact]
    public void ToPayload_survives_a_push_with_nothing_filled_in()
    {
        // A serialization failure here would take down the whole poll, so the empty case is worth pinning down.
        Assert.Equal(JsonValueKind.Object, PushPayloadFactory.ToPayload(new GitPush()).ValueKind);
    }

    [Fact]
    public void IsWatched_accepts_every_repository_when_none_are_configured()
    {
        Assert.True(PushPayloadFactory.IsWatched(new GitRepository { Name = "MyRepo" }, []));
    }

    [Fact]
    public void IsWatched_matches_a_configured_repository_by_name_ignoring_case()
    {
        Assert.True(PushPayloadFactory.IsWatched(new GitRepository { Name = "MyRepo" }, ["myrepo"]));
    }

    [Fact]
    public void IsWatched_matches_a_configured_repository_by_id()
    {
        var id = Guid.NewGuid();

        Assert.True(PushPayloadFactory.IsWatched(new GitRepository { Id = id, Name = "MyRepo" }, [id.ToString()]));
    }

    [Fact]
    public void IsWatched_rejects_a_repository_that_is_not_configured()
    {
        Assert.False(PushPayloadFactory.IsWatched(new GitRepository { Name = "Other" }, ["MyRepo"]));
    }
}
