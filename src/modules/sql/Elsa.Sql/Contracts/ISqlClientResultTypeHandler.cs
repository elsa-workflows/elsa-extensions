using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypeHandler
{
    /// <summary>
    /// Handles the result of a SQL query execution and transforms it into the desired format or type.
    /// </summary>
    /// <param name="queryResult">The result of the SQL query execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The transformed result.</returns>
    Task<object?> HandleAsync(object? queryResult, CancellationToken cancellationToken);
}
