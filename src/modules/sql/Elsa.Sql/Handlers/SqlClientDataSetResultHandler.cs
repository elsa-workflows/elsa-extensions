using System.Data;
using Elsa.Sql.Contracts;

namespace Elsa.Sql.Handlers;

/// <summary>
/// Handler for query results of type <see cref="DataSet"/>. This handler simply returns the raw <see cref="DataSet"/> object as the result of the query. It does not perform any transformation or processing on the data, allowing consumers to work with the full capabilities of the <see cref="DataSet"/> class when handling the query results.
/// </summary>
public class SqlClientDataSetResultHandler : ISqlClientResultTypeHandler
{
    public const string Name = "DataSet";

    public Task<object?> HandleAsync(object? queryResult, CancellationToken cancellationToken)
    {
        if (queryResult is null)
            return Task.FromResult<object?>(null);

        if (queryResult is not DataSet dataSet)
            throw new InvalidOperationException($"Expected query result to be of type {typeof(DataSet).FullName}, but got {queryResult.GetType().FullName}.");

        return Task.FromResult<object?>(dataSet);
    }
}
