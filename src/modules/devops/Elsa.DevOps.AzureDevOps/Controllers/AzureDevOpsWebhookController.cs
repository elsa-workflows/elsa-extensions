using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Controllers;

/// <summary>
/// Receives Azure DevOps Service Hook webhooks.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("webhooks/azure-devops")]
public sealed class AzureDevOpsWebhookController(
    IAzureDevOpsSecretReader secrets,
    AzureDevOpsWebhookEventHandler eventHandler,
    IOptions<AzureDevOpsOptions> options,
    ILogger<AzureDevOpsWebhookController> logger) : ControllerBase
{
    public const string BasicAuthSecretName = "AzureDevOps:WebhookBasicAuth";
    private static readonly JsonSerializerOptions SecretSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        AzureDevOpsWebhookDiagnosticsOptions diagnostics = options.Value.WebhookDiagnostics;
        bool passwordOnly = options.Value.WebhookAuthentication.PasswordOnly;
        LogLevel level = diagnostics.Level;
        bool diagnosing = logger.IsEnabled(level);

        string? basicAuthSecret = await secrets.GetSecretAsync(BasicAuthSecretName, cancellationToken).ConfigureAwait(false);
        BasicAuthCredentials? expectedCredentials = DeserializeCredentials(basicAuthSecret);
        string authorizationHeader = Request.Headers.Authorization.ToString();

        if (!IsValidBasicAuthentication(authorizationHeader, expectedCredentials, passwordOnly))
        {
            if (diagnosing)
            {
                // Which half is wrong is the whole question when one subscription delivers and another does not, and
                // neither half can be printed. The fingerprints are comparable without being reversible: equal
                // fingerprints mean equal values, so they say whether this delivery disagrees with the secret at all,
                // and whether two failing subscriptions disagree with it in the same way.
                (string? username, string? password) = ReadBasicCredentials(authorizationHeader);
                logger.Log(
                    level,
                    "Azure DevOps webhook delivery rejected with 401 because {Reason}. Secret {SecretName} {SecretState}; Authorization header {HeaderState}. Username sent {SentUsername} against expected {ExpectedUsername}; password fingerprint sent {SentPassword} against expected {ExpectedPassword}.",
                    DescribeRejection(basicAuthSecret, expectedCredentials, authorizationHeader, username, passwordOnly),
                    BasicAuthSecretName,
                    basicAuthSecret == null ? "was not found" : expectedCredentials == null ? "was found but does not hold valid JSON with a username and a password" : "was found and parsed",
                    string.IsNullOrWhiteSpace(authorizationHeader) ? "is absent" : authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase) ? "is Basic" : "is not Basic",
                    username == null ? "(none)" : $"'{username}'",
                    expectedCredentials?.Username == null ? "(none)" : $"'{expectedCredentials.Username}'",
                    Fingerprint(password),
                    Fingerprint(expectedCredentials?.Password));
            }

            return Unauthorized();
        }

        if (passwordOnly)
        {
            // Deliberately a warning, and deliberately not behind the diagnostics level: this is the endpoint running
            // on one shared password with no second factor of any kind, and the one way that ends badly is by being
            // forgotten. One line per delivery is a small price, and it stops the moment the switch goes back off.
            logger.LogWarning(
                "The Azure DevOps webhook endpoint accepted a delivery on its password alone. AzureDevOps:WebhookAuthentication:PasswordOnly is on, so the username is not checked. Switch it off once the subscriptions carry a username.");
        }

        // Read as text rather than straight off the stream, so the delivered message can be logged exactly as it
        // arrived. Azure DevOps posts once and never again, so a payload that turns out to be the explanation is only
        // available if it was kept the first time.
        string body;

        using (StreamReader reader = new(Request.Body, Encoding.UTF8, leaveOpen: true))
            body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (diagnosing && diagnostics.IncludePayloads)
            logger.Log(level, "Azure DevOps webhook delivery body ({Length} characters): {Body}", body.Length, Truncate(body, diagnostics.MaxPayloadLength));

        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(body);
        string? eventType = GetString(payload, "eventType");

        if (string.IsNullOrWhiteSpace(eventType))
        {
            if (diagnosing)
                logger.Log(level, "Azure DevOps webhook delivery rejected with 400: the payload carries no eventType.");

            return BadRequest("The Azure DevOps webhook payload does not contain an eventType.");
        }

        JsonElement resource = payload.TryGetProperty("resource", out JsonElement resourceElement)
            ? resourceElement.Clone()
            : payload.Clone();

        AzureDevOpsWebhookEvent message = new(
            eventType,
            resource,
            GetNestedString(payload, "resource", "project", "id")
                ?? GetNestedString(payload, "resource", "repository", "project", "id")
                // Work item events do not carry the project on the resource itself.
                ?? GetNestedString(payload, "resourceContainers", "project", "id"),
            GetNestedString(payload, "resource", "project", "name")
                ?? GetNestedString(payload, "resource", "repository", "project", "name"),
            GetString(payload, "resourceVersion"),
            GetString(payload, "publisherId"));

        if (diagnosing)
        {
            // The project identifiers are singled out because they are what a work item trigger is matched on, and a
            // work item event carries the project nowhere near where the other events carry it: the ID comes from
            // resourceContainers, and the name only exists as the System.TeamProject field, which Azure DevOps leaves
            // out unless the subscription sends all resource details.
            logger.Log(
                level,
                "Azure DevOps webhook delivery accepted: eventType {EventType}, project ID {ProjectId}, project name {ProjectName}, resourceVersion {ResourceVersion}, publisherId {PublisherId}.",
                message.EventType,
                message.ProjectId ?? "(none)",
                message.ProjectName ?? "(none)",
                message.ResourceVersion ?? "(none)",
                message.PublisherId ?? "(none)");
        }

        await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }

    private static bool IsValidBasicAuthentication(string? authorizationHeader, BasicAuthCredentials? expectedCredentials, bool passwordOnly)
    {
        if (expectedCredentials?.Password is null)
            return false;

        // Only when the username is still part of the check: under password-only the username in the secret is not
        // consulted at all, so requiring it to be filled in would refuse deliveries for a value nothing reads.
        if (!passwordOnly && string.IsNullOrEmpty(expectedCredentials.Username))
            return false;

        (string? providedUsername, string? providedPassword) = ReadBasicCredentials(authorizationHeader);

        if (providedPassword == null || !FixedTimeEquals(providedPassword, expectedCredentials.Password))
            return false;

        return passwordOnly || (providedUsername != null && FixedTimeEquals(providedUsername, expectedCredentials.Username));
    }

    /// <summary>
    /// Says which of the checks turned this delivery away, in the order they are applied.
    /// </summary>
    /// <remarks>
    /// Without this the log can contradict itself. An empty configured username is refused before anything is
    /// compared, so the line would otherwise report a matching username and a matching password fingerprint next to a
    /// 401 and leave the reader with nowhere to go - which is exactly what happens after someone blanks the username
    /// in the secret to match a subscription that sends none, a repair that cannot work and quietly breaks every
    /// subscription that was configured correctly.
    /// </remarks>
    private static string DescribeRejection(string? secret, BasicAuthCredentials? expected, string? authorizationHeader, string? sentUsername, bool passwordOnly)
    {
        if (secret == null)
            return $"the secret {BasicAuthSecretName} does not exist";

        if (expected == null)
            return "the secret does not hold valid JSON with a username and a password";

        if (expected.Password == null)
            return "the secret holds no password";

        if (!passwordOnly && string.IsNullOrEmpty(expected.Username))
            return "the username in the secret is empty, which is refused outright - fill the username in on the subscription rather than blanking it here, or switch AzureDevOps:WebhookAuthentication:PasswordOnly on deliberately";

        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return "the delivery carried no Authorization header";

        if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return "the Authorization header is not Basic";

        if (sentUsername == null)
            return "the Basic credentials could not be decoded";

        if (passwordOnly)
            return "the password does not match the secret; the username is not being checked";

        return string.IsNullOrEmpty(sentUsername)
            ? "the delivery sent no username, which Azure DevOps does when the subscription's username field is left blank"
            : "the credentials sent do not match the secret";
    }

    /// <summary>
    /// Splits a Basic Authentication header into the username and password it carries, or <c>(null, null)</c> when it
    /// is absent, is not Basic, or does not decode.
    /// </summary>
    private static (string? Username, string? Password) ReadBasicCredentials(string? authorizationHeader)
    {
        const string basicPrefix = "Basic ";

        if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith(basicPrefix, StringComparison.OrdinalIgnoreCase))
            return (null, null);

        try
        {
            string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[basicPrefix.Length..].Trim()));
            int separatorIndex = credentials.IndexOf(':');

            return separatorIndex < 0
                ? (null, null)
                : (credentials[..separatorIndex], credentials[(separatorIndex + 1)..]);
        }
        catch (FormatException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// A short, stable stand-in for a credential, so two of them can be compared in a log without either being
    /// written to it.
    /// </summary>
    private static string Fingerprint(string? value) =>
        value == null ? "(none)" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..8];

    private static string Truncate(string value, int maxLength) =>
        maxLength > 0 && value.Length > maxLength
            ? $"{value[..maxLength]}… ({value.Length - maxLength} more characters)"
            : value;

    private static BasicAuthCredentials? DeserializeCredentials(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BasicAuthCredentials>(value, SecretSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record BasicAuthCredentials(string Username, string Password);

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? GetNestedString(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out element))
                return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
}
