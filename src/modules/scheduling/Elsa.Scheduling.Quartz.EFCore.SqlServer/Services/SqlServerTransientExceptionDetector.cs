using Elsa.Scheduling.Quartz.Contracts;
using Microsoft.Data.SqlClient;

namespace Elsa.Scheduling.Quartz.EFCore.SqlServer.Services;

/// <summary>
/// Detects transient exceptions specific to SQL Server based on error numbers.
/// </summary>
public class SqlServerTransientExceptionDetector : ITransientExceptionDetector
{
    // SQL Server error numbers that indicate transient failures
    // See: https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
    private static readonly HashSet<int> TransientErrorNumbers = new()
    {
        // Connectivity errors
        -1,     // Connection timeout
        -2,     // Timeout expired
        2,      // Network error
        53,     // Server not found or not accessible
        64,     // Error in SSL communication
        233,    // Connection initialization error
        10053,  // Transport-level error (connection forcibly closed)
        10054,  // Connection forcibly closed by remote host
        10060,  // Connection timeout
        10061,  // Connection refused
        11001,  // Host not found
        20,     // Instance not found

        // Resource errors
        1205,   // Deadlock victim
        40197,  // Service error processing request
        40501,  // Service busy
        40613,  // Database unavailable
        49918,  // Cannot process request (insufficient resources)
        49919,  // Cannot process create/update request (too many operations in progress)
        49920,  // Cannot process request (too many operations in progress)
    };

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        return exception is SqlException sqlException && sqlException.Errors.Cast<SqlError>().Any(error => TransientErrorNumbers.Contains(error.Number));
    }
}
