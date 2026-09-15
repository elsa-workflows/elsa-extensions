using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Reads an Elsa secret by name, yielding <c>null</c> when no secret of that name exists.
/// </summary>
/// <remarks>
/// <see cref="ISecretResolver"/> looks like the dependency to take, but it throws when the secret is absent, and
/// absence is the ordinary case here rather than a fault. The token lookup walks several rungs - the secret named
/// after the calling user, then the configured default - and has to be able to pass over an empty one; the webhook
/// endpoint has to be able to report "no credential is configured" rather than fail the request with a 500. So this
/// goes to <see cref="ISecretManager"/>, whose <c>GetAsync</c> returns <c>null</c> for a name it does not know, and
/// leaves the exception for what genuinely is one.
/// </remarks>
public interface IAzureDevOpsSecretReader
{
    /// <summary>
    /// The value of the secret named <paramref name="name"/>, or <c>null</c> when there is no such secret or it holds
    /// no value.
    /// </summary>
    ValueTask<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class AzureDevOpsSecretReader(ISecretManager secretManager) : IAzureDevOpsSecretReader
{
    /// <inheritdoc />
    public async ValueTask<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        Secret? secret = await secretManager.GetAsync(name, cancellationToken).ConfigureAwait(false);

        if (secret == null)
            return null;

        SecretPayload payload = await secretManager.ResolvePayloadAsync(secret, cancellationToken).ConfigureAwait(false);

        return payload.Value;
    }
}
