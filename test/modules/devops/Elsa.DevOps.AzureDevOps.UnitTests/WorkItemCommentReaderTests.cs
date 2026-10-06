using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// A Service Hook does not send a comment as a comment: it sends the work item with the text in
/// <c>System.History</c>, and the author in whichever of several shapes that resource version happens to use. These
/// cover the shapes, which is the part that cannot be checked by reading the code.
/// </summary>
public class WorkItemCommentReaderTests
{
    [Fact]
    public void ReadsTheCommentOutOfAServiceHookPayload()
    {
        PostedComment comment = Assert.IsType<PostedComment>(WorkItemCommentReader.FromPayload(Json("""
            {
              "id": 41290,
              "rev": 7,
              "fields": {
                "System.WorkItemType": "Bug",
                "System.History": "Kun je hier even naar kijken @studio?",
                "System.ChangedBy": "Alice Anderson <alice@contoso.com>",
                "System.ChangedDate": "2026-08-12T09:14:02Z"
              }
            }
            """)));

        Assert.Equal("Kun je hier even naar kijken @studio?", comment.Text);
        Assert.Equal("Alice Anderson", comment.Author);
        Assert.Equal("alice@contoso.com", comment.AuthorUniqueName);
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 9, 14, 2, TimeSpan.Zero), comment.CreatedOn);

        // A Service Hook payload carries no comment id, and pretending otherwise would suggest a comment that could
        // be fetched or replied to.
        Assert.Null(comment.Id);
    }

    [Fact]
    public void PrefersTheIdentityObjectOverTheFieldValue()
    {
        // revisedBy is an identity object, so it carries the sign-in name reliably where a field value only sometimes
        // does.
        PostedComment comment = Assert.IsType<PostedComment>(WorkItemCommentReader.FromPayload(Json("""
            {
              "revisedBy": { "displayName": "Zara", "uniqueName": "zara@contoso.nl" },
              "revisedDate": "2026-08-12T10:00:00Z",
              "fields": {
                "System.History": "Ik kijk ernaar.",
                "System.ChangedBy": "Iemand Anders <anders@contoso.nl>"
              }
            }
            """)));

        Assert.Equal("Zara", comment.Author);
        Assert.Equal("zara@contoso.nl", comment.AuthorUniqueName);
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero), comment.CreatedOn);
    }

    [Fact]
    public void ReadsAChangedFieldOutOfItsOldAndNewValueEnvelope()
    {
        // A changed field arrives as an object rather than a plain value, which is the shape that would otherwise
        // silently yield no comment at all.
        PostedComment comment = Assert.IsType<PostedComment>(WorkItemCommentReader.FromPayload(Json("""
            {
              "fields": {
                "System.History": { "newValue": "Nieuwe opmerking" },
                "System.ChangedBy": { "newValue": { "displayName": "Alice", "uniqueName": "alice@contoso.com" } }
              }
            }
            """)));

        Assert.Equal("Nieuwe opmerking", comment.Text);
        Assert.Equal("Alice", comment.Author);
        Assert.Equal("alice@contoso.com", comment.AuthorUniqueName);
    }

    [Fact]
    public void ReadsAnAuthorThatIsOnlyADisplayName()
    {
        PostedComment comment = Assert.IsType<PostedComment>(WorkItemCommentReader.FromPayload(Json("""
            { "fields": { "System.History": "hoi", "System.ChangedBy": "Alice Anderson" } }
            """)));

        Assert.Equal("Alice Anderson", comment.Author);
        Assert.Null(comment.AuthorUniqueName);
    }

    [Fact]
    public void ReportsNoCommentWhenThePayloadCarriesNoHistory()
    {
        // The honest answer for an event that turns out not to be about a comment.
        Assert.Null(WorkItemCommentReader.FromPayload(Json("""{ "fields": { "System.Title": "Iets" } }""")));
    }

    [Theory]
    [InlineData("""{ "fields": { "System.History": "   " } }""")]
    [InlineData("""{ "fields": {} }""")]
    [InlineData("""{}""")]
    [InlineData("""[]""")]
    public void ReportsNoCommentForAPayloadWithNothingToRead(string payload)
    {
        Assert.Null(WorkItemCommentReader.FromPayload(Json(payload)));
    }

    [Fact]
    public void ReportsNoCommentForAPayloadItDoesNotUnderstand()
    {
        Assert.Null(WorkItemCommentReader.FromPayload(null));
        Assert.Null(WorkItemCommentReader.FromPayload("a string"));
    }

    [Fact]
    public void ReadsTheCommentOutOfAPolledWorkItem()
    {
        // The poller normally carries the comment on the event, but a work item that does have System.History is
        // still worth reading rather than dropping.
        WorkItem workItem = new()
        {
            Id = 41290,
            Fields = new Dictionary<string, object>
            {
                ["System.History"] = "Vanuit de poller",
                ["System.ChangedBy"] = "Alice <alice@contoso.com>",
                ["System.ChangedDate"] = "2026-08-12T09:14:02Z",
            },
        };

        PostedComment comment = Assert.IsType<PostedComment>(WorkItemCommentReader.FromPayload(workItem));

        Assert.Equal("Vanuit de poller", comment.Text);

        // Split the same way the JSON path splits it: which of the two shapes the field travelled in should not
        // change what the workflow is handed.
        Assert.Equal("Alice", comment.Author);
        Assert.Equal("alice@contoso.com", comment.AuthorUniqueName);
    }

    [Fact]
    public void MapsACommentTheApiReturned()
    {
        Comment comment = new()
        {
            Id = 7,
            Text = "Het echte commentaar",
            CreatedBy = new IdentityRef { DisplayName = "Alice Anderson", UniqueName = "alice@contoso.com" },
            CreatedDate = new DateTime(2026, 8, 12, 9, 14, 2, DateTimeKind.Unspecified),
        };

        PostedComment mapped = Assert.IsType<PostedComment>(WorkItemCommentReader.FromApi(comment));

        // This is the path that knows everything, id included - which is the whole reason the poller carries its
        // comment on the event instead of letting the trigger recover it from the payload.
        Assert.Equal(7, mapped.Id);
        Assert.Equal("Het echte commentaar", mapped.Text);
        Assert.Equal("Alice Anderson", mapped.Author);
        Assert.Equal("alice@contoso.com", mapped.AuthorUniqueName);
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 9, 14, 2, TimeSpan.Zero), mapped.CreatedOn);
    }

    [Fact]
    public void ReportsNoCommentForAnApiCommentWithNoText()
    {
        Assert.Null(WorkItemCommentReader.FromApi(null));
        Assert.Null(WorkItemCommentReader.FromApi(new Comment { Id = 7, Text = "  " }));
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();
}
