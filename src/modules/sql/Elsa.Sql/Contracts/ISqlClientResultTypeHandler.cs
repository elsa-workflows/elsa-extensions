using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypeHandler
{
    Task<object?> HandleAsync(object? queryResult, CancellationToken cancellationToken);
}
