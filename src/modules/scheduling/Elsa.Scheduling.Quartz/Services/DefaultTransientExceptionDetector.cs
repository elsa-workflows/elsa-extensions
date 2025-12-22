using Elsa.Scheduling.Quartz.Contracts;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation that detects common transient exceptions from the .NET framework.
/// </summary>
public class DefaultTransientExceptionDetector : ITransientExceptionDetector
{
    private static readonly HashSet<string> TransientExceptionTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common framework exceptions
        "HttpRequestException",
        "TimeoutException",
        "TaskCanceledException",
        "IOException",
        "SocketException",
        "EndOfStreamException",
        
        // Database-related transient exceptions (by name, not type reference)
        "DbException",
        "SqlException",
        "NpgsqlException",
        "MongoConnectionException",
        "MongoExecutionTimeoutException",
        "MongoNodeIsRecoveringException",
        "MongoNotPrimaryException",
        "MySqlException",
        
        // Network-related exceptions
        "HttpIOException",
        "WebException",
    };
    
    private static readonly HashSet<string> TransientExceptionMessagePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeout",
        "timed out",
        "connection reset",
        "connection refused",
        "broken pipe",
        "network",
        "end of stream",
        "attempted to read past the end",
    };

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        // Check if the exception type name matches any known transient exception
        var exceptionTypeName = exception.GetType().Name;
        if (TransientExceptionTypeNames.Contains(exceptionTypeName))
            return true;
        
        // Check if the exception message contains any transient patterns
        if (!string.IsNullOrEmpty(exception.Message))
        {
            foreach (var pattern in TransientExceptionMessagePatterns)
            {
                if (exception.Message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        
        return false;
    }
}
