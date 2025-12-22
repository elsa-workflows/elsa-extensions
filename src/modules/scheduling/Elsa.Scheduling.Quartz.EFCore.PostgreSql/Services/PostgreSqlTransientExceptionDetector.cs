using Elsa.Scheduling.Quartz.Contracts;
using Npgsql;

namespace Elsa.Scheduling.Quartz.EFCore.PostgreSql.Services;

/// <summary>
/// Detects transient exceptions specific to PostgreSQL/Npgsql based on error codes.
/// </summary>
public class PostgreSqlTransientExceptionDetector : ITransientExceptionDetector
{
    // PostgreSQL error codes that indicate transient failures
    // See: https://www.postgresql.org/docs/current/errcodes-appendix.html
    private static readonly HashSet<string> TransientErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "08000", // connection_exception
        "08003", // connection_does_not_exist
        "08006", // connection_failure
        "08001", // sqlclient_unable_to_establish_sqlconnection
        "08004", // sqlserver_rejected_establishment_of_sqlconnection
        "08007", // transaction_resolution_unknown
        "40001", // serialization_failure
        "40P01", // deadlock_detected
        "53000", // insufficient_resources
        "53100", // disk_full
        "53200", // out_of_memory
        "53300", // too_many_connections
        "57P03", // cannot_connect_now
        "58000", // system_error
        "58030", // io_error
    };

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        return exception is NpgsqlException { SqlState: not null } npgsqlException && TransientErrorCodes.Contains(npgsqlException.SqlState);
    }
}
