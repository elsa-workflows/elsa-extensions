namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Helpers for validating activity inputs. Use Try* in CanExecuteAsync; use Throw* in ExecuteAsync/GetConnection as a safety net.
/// </summary>
internal static class ActivityInputValidation
{
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
