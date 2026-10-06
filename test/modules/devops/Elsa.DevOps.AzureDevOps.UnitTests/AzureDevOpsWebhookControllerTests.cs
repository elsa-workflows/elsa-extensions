using System.Text;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Controllers;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the endpoint's two gates - the shared password and the eventType - and, above all, that the delivered
/// message still survives them. The credential check and the body are read from the same request, and reading the
/// body as text so it can be logged is exactly the kind of change that can leave the parse with an empty stream: the
/// endpoint would then answer 401 or 500 for every real delivery, and Azure DevOps would disable the subscription
/// before anyone noticed.
/// </summary>
public class AzureDevOpsWebhookControllerTests
{
    private const string Username = "azure-devops";
    private const string Password = "a-shared-password";
    private const string ProjectId = "1ab86e69-f06e-41d1-8059-0844535a9243";
    private const string ProjectName = "Contoso";

    private readonly IStimulusSender _stimulusSender = Substitute.For<IStimulusSender>();
    private readonly IAzureDevOpsSecretReader _secretProvider = Substitute.For<IAzureDevOpsSecretReader>();

    private const string WorkItemUpdatedPayload = """
        {
          "eventType": "workitem.updated",
          "publisherId": "tfs",
          "resourceVersion": "1.0",
          "resource": {
            "workItemId": 42,
            "revision": { "fields": { "System.TeamProject": "Contoso", "System.WorkItemType": "Bug" } }
          },
          "resourceContainers": { "project": { "id": "1ab86e69-f06e-41d1-8059-0844535a9243" } }
        }
        """;

    private AzureDevOpsWebhookController CreateController(string body, string? username = Username, string? password = Password, string secretUsername = Username, bool passwordOnly = false)
    {
        _secretProvider.GetSecretAsync(AzureDevOpsWebhookController.BasicAuthSecretName, Arg.Any<CancellationToken>())
            .Returns($$"""{"username":"{{secretUsername}}","password":"{{Password}}"}""");

        DefaultHttpContext httpContext = new();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        if (username != null && password != null)
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            httpContext.Request.Headers.Authorization = $"Basic {credentials}";
        }

        AzureDevOpsWebhookEventHandler handler = new(_stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        return new AzureDevOpsWebhookController(
            _secretProvider,
            handler,
            Options.Create(new AzureDevOpsOptions
            {
                WebhookAuthentication = new AzureDevOpsWebhookAuthenticationOptions { PasswordOnly = passwordOnly },
            }),
            NullLogger<AzureDevOpsWebhookController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task AcceptsADeliveryWhoseCredentialsMatchTheSecretAndDispatchesIt()
    {
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);

        // The dispatch is the point: a 202 on its own would also be returned by an endpoint that read an empty body
        // and matched nothing, which is the failure this test exists to catch.
        Assert.NotEmpty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task RejectsADeliveryWhosePasswordDiffersFromTheSecret()
    {
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, password: "not-the-shared-password");

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task RejectsEveryDeliveryWhenTheUsernameInTheSecretIsBlank()
    {
        // Azure DevOps sends "Basic base64(:password)" when the subscription's username field is left empty, and the
        // obvious repair is to blank the username in the secret to match. It cannot work - an empty configured
        // username is refused before anything is compared - and it is worse than not working: it also turns away every
        // subscription that was configured correctly, which is how a working comment trigger goes dark alongside the
        // broken one. Fill the username in on the subscription instead.
        //
        // If this ever starts returning Accepted, the endpoint has begun authenticating on the password alone. That is
        // a deliberate decision to make in the open, not something to let a test start allowing quietly.
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: string.Empty, secretUsername: string.Empty);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task RejectsADeliveryThatSendsNoUsernameAgainstAConfiguredOne()
    {
        // The delivery that started this: the right password with an empty username, from a subscription whose
        // username field was never filled in.
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: string.Empty);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task AcceptsADeliveryWithoutAUsernameWhenPasswordOnlyIsSwitchedOn()
    {
        // The subscription that was saved with an empty username field, under the switch that says the username is
        // not part of the check.
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: string.Empty, passwordOnly: true);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.NotEmpty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task AcceptsADeliveryUnderPasswordOnlyEvenWhenTheSecretsUsernameIsBlank()
    {
        // Under the strict check this combination is refused before anything is compared. The switch has to lift that
        // too, or it would refuse deliveries over a value it has just declared irrelevant.
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: string.Empty, secretUsername: string.Empty, passwordOnly: true);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task StillRejectsAWrongPasswordUnderPasswordOnly()
    {
        // The point of the switch is that the password carries the whole weight, so this is the assertion that gives
        // it any value at all. If it ever fails, the endpoint is open.
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: string.Empty, password: "not-the-shared-password", passwordOnly: true);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task StillRejectsADeliveryWithNoAuthorizationHeaderUnderPasswordOnly()
    {
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: null, password: null, passwordOnly: true);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task RejectsADeliveryThatCarriesNoAuthorizationHeader()
    {
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload, username: null, password: null);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task RefusesAPayloadWithoutAnEventType()
    {
        AzureDevOpsWebhookController controller = CreateController("""{ "resource": { "workItemId": 42 } }""");

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(_stimulusSender.ReceivedCalls());
    }

    [Fact]
    public async Task MatchesAWorkItemEventOnBothTheProjectIdAndTheProjectName()
    {
        AzureDevOpsWebhookController controller = CreateController(WorkItemUpdatedPayload);
        string activityTypeName = ActivityTypeNameHelper.GenerateTypeName(typeof(WorkItemUpdatedTrigger));

        _ = await controller.ReceiveAsync(CancellationToken.None);

        // A work item event carries no project on the resource: the ID only exists under resourceContainers, and the
        // name only as the System.TeamProject field on the revision. Both have to be matched, because a trigger is
        // indexed with whichever of the two its author supplied - and AzureDevOps:DefaultProject is a name.
        await _stimulusSender.Received(1).SendAsync(
            activityTypeName,
            new AzureDevOpsWebhookBookmark("workitem.updated", ProjectId),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
        await _stimulusSender.Received(1).SendAsync(
            activityTypeName,
            new AzureDevOpsWebhookBookmark("workitem.updated", ProjectName),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AcceptsACommentDeliveryUnderTheNameAzureDevOpsSends()
    {
        // This exact delivery answered 500 in production: the handler did not recognise workitem.commented and threw,
        // which Azure DevOps counts towards disabling the subscription. A comment must be accepted and dispatched.
        AzureDevOpsWebhookController controller = CreateController("""
            {
              "eventType": "workitem.commented",
              "resource": {
                "id": 41290,
                "fields": { "System.TeamProject": "Contoso", "System.WorkItemType": "Bug" }
              },
              "resourceContainers": { "project": { "id": "1ab86e69-f06e-41d1-8059-0844535a9243" } }
            }
            """);

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        await _stimulusSender.Received(1).SendAsync(
            ActivityTypeNameHelper.GenerateTypeName(typeof(WorkItemCommentedTrigger)),
            new AzureDevOpsWebhookBookmark(AzureDevOpsWebhookEventTypes.WorkItemCommented, ProjectName),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MatchesNothingOnTheProjectNameWhenTheDeliveryLeftTheFieldsOut()
    {
        // What a subscription set to minimal resource details delivers. It is accepted, it produces a bookmark, and
        // that bookmark carries only the project GUID - so a trigger indexed on the project *name* never matches and
        // the delivery is silently dropped. If this ever starts matching on the name, the payload shape changed and
        // the "202 but nothing runs" guidance in the README and the howto is out of date.
        AzureDevOpsWebhookController controller = CreateController("""
            {
              "eventType": "workitem.updated",
              "resource": { "workItemId": 42 },
              "resourceContainers": { "project": { "id": "1ab86e69-f06e-41d1-8059-0844535a9243" } }
            }
            """);
        string activityTypeName = ActivityTypeNameHelper.GenerateTypeName(typeof(WorkItemUpdatedTrigger));

        _ = await controller.ReceiveAsync(CancellationToken.None);

        await _stimulusSender.Received(1).SendAsync(
            activityTypeName,
            new AzureDevOpsWebhookBookmark("workitem.updated", ProjectId),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
        await _stimulusSender.DidNotReceive().SendAsync(
            activityTypeName,
            new AzureDevOpsWebhookBookmark("workitem.updated", ProjectName),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
    }
}
