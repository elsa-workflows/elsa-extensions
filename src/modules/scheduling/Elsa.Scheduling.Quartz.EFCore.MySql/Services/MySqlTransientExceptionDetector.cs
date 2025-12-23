using Elsa.Resilience.Contracts;
using MySqlConnector;

namespace Elsa.Scheduling.Quartz.EFCore.MySql.Services;

/// <summary>
/// Detects transient exceptions specific to MySQL based on error codes.
/// </summary>
public class MySqlTransientExceptionDetector : ITransientExceptionDetector
{
    // MySQL error codes that indicate transient failures
    // See: https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html
    private static readonly HashSet<int> TransientErrorCodes =
    [
        1040, // ER_CON_COUNT_ERROR - Too many connections
        1053, // ER_SERVER_SHUTDOWN - Server shutdown in progress
        1129, // ER_HOST_IS_BLOCKED - Host blocked due to too many connection errors
        1152, // ER_ABORTING_CONNECTION - Aborted connection
        1153, // ER_NET_PACKET_TOO_LARGE - Packet too large
        1154, // ER_NET_READ_ERROR_FROM_PIPE - Error reading from pipe
        1155, // ER_NET_FCNTL_ERROR - fcntl() error
        1156, // ER_NET_PACKETS_OUT_OF_ORDER - Packets out of order
        1157, // ER_NET_UNCOMPRESS_ERROR - Couldn't uncompress packet
        1158, // ER_NET_READ_ERROR - Error reading from network
        1159, // ER_NET_READ_INTERRUPTED - Network read interrupted
        1160, // ER_NET_ERROR_ON_WRITE - Error on network write
        1161, // ER_NET_WRITE_INTERRUPTED - Network write interrupted
        1205, // ER_LOCK_WAIT_TIMEOUT - Lock wait timeout exceeded
        1213, // ER_LOCK_DEADLOCK - Deadlock found
        2002, // CR_CONNECTION_ERROR - Can't connect to server
        2003, // CR_CONN_HOST_ERROR - Can't connect to MySQL server
        2006, // CR_SERVER_GONE_ERROR - MySQL server has gone away
        2013 // CR_SERVER_LOST - Lost connection during query
    ];

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        return exception is MySqlException mySqlException && TransientErrorCodes.Contains(mySqlException.Number);
    }
}
