namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// How the Service Hook endpoint decides that a delivery really came from Azure DevOps.
/// </summary>
public class AzureDevOpsWebhookAuthenticationOptions
{
    /// <summary>
    /// Accept a delivery on its password alone, ignoring the username on both sides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Off by default, and worth leaving off. On, the endpoint is guarded by one shared password and nothing else, so
    /// that password carries the entire weight and should be long and random.
    /// </para>
    /// <para>
    /// It exists because Azure DevOps lets a subscription be saved with an empty <c>Basic authentication username</c>
    /// and then delivers <c>Basic base64(":password")</c>. Correcting that on the subscription is the better repair -
    /// the username field is one box - and blanking the username in the secret to match is not a repair at all: an
    /// empty configured username is refused outright, which takes down every subscription that was set up correctly
    /// along with the one that was not. This switch is the honest version of that intent: it says the username is not
    /// part of the check, rather than leaving a check in place that cannot pass.
    /// </para>
    /// </remarks>
    public bool PasswordOnly { get; set; }
}
