namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Helpers for validating activity inputs.
/// </summary>
internal static class ActivityInputValidation
{
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
