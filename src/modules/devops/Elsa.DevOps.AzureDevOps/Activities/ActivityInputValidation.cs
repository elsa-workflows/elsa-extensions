using Elsa.Extensions;
using Elsa.Workflows;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Helpers for validating activity inputs. Use Try* in CanExecuteAsync and hand the result to
/// <see cref="ThrowIfInvalid"/>; use Throw* in ExecuteAsync/GetConnection as a safety net.
/// </summary>
internal static class ActivityInputValidation
{
    /// <summary>
    /// Reports a refused precondition and stops the run, leaving the message on the execution log either way.
    /// </summary>
    /// <remarks>
    /// Throwing is what makes a misconfigured activity visible, and returning <c>false</c> from <c>CanExecuteAsync</c>
    /// is not: Elsa skips <c>ExecuteAsync</c> for an activity that refuses, and a skipped activity never signals
    /// completion, so its parent container waits forever and the instance sits <c>Running</c>/<c>Suspended</c> with no
    /// fault to show for it. Deliberately a copy of the same helper in <c>Elsa.Ai.Agent</c>, whose remarks carry the
    /// full account and name the test that pins the behaviour down.
    /// </remarks>
    public static void ThrowIfInvalid(ActivityExecutionContext context, (bool Valid, string? Error) check)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (check.Valid)
            return;

        context.AddExecutionLogEntry("Precondition Failed", check.Error);

        throw new ArgumentException(check.Error);
    }

    public static (bool Valid, string? Error) TryValidateRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, $"'{parameterName}' must be specified and non-empty.");
        return (true, null);
    }

    public static (bool Valid, string? Error) TryValidateUri(string? value, string parameterName)
    {
        var (requiredOk, requiredErr) = TryValidateRequired(value, parameterName);
        if (!requiredOk) return (false, requiredErr);
        if (!Uri.TryCreate(value!.Trim(), UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri || (uri.Scheme != "http" && uri.Scheme != "https"))
            return (false, $"'{parameterName}' must be a valid HTTP or HTTPS URL.");
        return (true, null);
    }

    public static (bool Valid, string? Error) TryValidateNonNegative(int value, string parameterName)
    {
        if (value < 0)
            return (false, $"'{parameterName}' must be non-negative.");
        return (true, null);
    }

    public static (bool Valid, string? Error) TryValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
            return (false, $"'{parameterName}' must be greater than zero.");
        return (true, null);
    }

    public static void ThrowIfNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{parameterName}' must be specified and non-empty.", parameterName);
    }

    public static void ThrowIfInvalidUri(string? value, string parameterName)
    {
        ThrowIfNullOrEmpty(value, parameterName);
        if (!Uri.TryCreate(value!.Trim(), UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException($"'{parameterName}' must be a valid HTTP or HTTPS URL.", parameterName);
    }

    public static void ThrowIfNegative(int value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName, value, $"'{parameterName}' must be non-negative.");
    }

    public static void ThrowIfNegativeOrZero(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, $"'{parameterName}' must be greater than zero.");
    }
}
