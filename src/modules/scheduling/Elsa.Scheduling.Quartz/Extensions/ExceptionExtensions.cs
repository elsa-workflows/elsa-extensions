using Elsa.Scheduling.Quartz.Contracts;

namespace Elsa.Scheduling.Quartz.Extensions;

/// <summary>
/// Extension methods for <see cref="Exception"/>.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Determines whether the specified exception or any of its inner exceptions is a transient exception that may be resolved by retrying.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <param name="detectors">The collection of transient exception detectors to use.</param>
    /// <returns>True if the exception or any inner exception is transient; otherwise, false.</returns>
    public static bool IsTransient(this Exception exception, IEnumerable<ITransientExceptionDetector> detectors)
    {
        var detectorsList = detectors.ToList();

        // Walk the entire exception chain (including inner exceptions)
        var currentException = exception;
        while (currentException != null)
        {
            // Check if any detector identifies this exception as transient
            if (detectorsList.Any(detector => detector.IsTransient(currentException)))
                return true;

            currentException = currentException.InnerException;
        }

        // Also check aggregate exceptions
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (innerException.IsTransient(detectorsList))
                    return true;
            }
        }

        return false;
    }
}
