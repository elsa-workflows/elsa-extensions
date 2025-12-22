using Elsa.Scheduling.Quartz.Contracts;
using Microsoft.Data.Sqlite;

namespace Elsa.Scheduling.Quartz.EFCore.Sqlite.Services;

/// <summary>
/// Detects transient exceptions specific to SQLite based on error codes.
/// </summary>
public class SqliteTransientExceptionDetector : ITransientExceptionDetector
{
    // SQLite error codes that indicate transient failures
    // See: https://www.sqlite.org/rescode.html
    private static readonly HashSet<int> TransientErrorCodes = new()
    {
        5,    // SQLITE_BUSY - Database is locked
        6,    // SQLITE_LOCKED - Database table is locked
        10,   // SQLITE_IOERR - Disk I/O error
        11,   // SQLITE_CORRUPT - Database disk image is malformed (can be transient during writes)
        13,   // SQLITE_FULL - Database or disk is full
        14,   // SQLITE_CANTOPEN - Unable to open database file
        23,   // SQLITE_AUTH - Authorization denied (can be transient with WAL mode)
    };

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        return exception is SqliteException sqliteException && TransientErrorCodes.Contains(sqliteException.SqliteErrorCode);
    }
}
