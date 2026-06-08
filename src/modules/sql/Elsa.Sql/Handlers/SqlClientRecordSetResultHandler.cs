using System.Data;
using Elsa.Extensions;
using Elsa.Sql.Contracts;

namespace Elsa.Sql.Handlers;

/// <summary>
/// Handler for query results of type "RecordSet". This handler transforms the raw query result, which is expected to be a <see cref="DataSet"/> containing a single <see cref="DataTable"/>, into an array of records. Each record is represented as a dictionary where the keys are the column names and the values are the corresponding values for each column in that row. This allows consumers to work with the query results in a more flexible and dynamic way, without being tied to the structure of a DataSet or DataTable.
/// </summary>
public class SqlClientRecordSetResultHandler : ISqlClientResultTypeHandler
{
    public const string Name = "RecordSet";

    public Task<object?> HandleAsync(object? queryResult, CancellationToken cancellationToken)
    {
        if (queryResult is null)
            return Task.FromResult<object?>(null);

        if (queryResult is not DataSet dataSet)
            throw new InvalidOperationException($"Expected query result to be of type {typeof(DataSet).FullName}, but got {queryResult.GetType().FullName}.");

        if (dataSet.Tables.Count == 0)
            return Task.FromResult<object?>(null);

        if (dataSet.Tables.Count > 1)
            throw new InvalidOperationException($"Expected query result to contain only one DataTable, but got {dataSet.Tables.Count}.");

        DataTable dataTable = dataSet.Tables[0];

        List<Dictionary<string, object?>> result = new List<Dictionary<string, object?>>(dataTable.Rows.Count);
        IEnumerable<DataColumn> columns = dataTable.Columns.Cast<DataColumn>();
        foreach (DataRow dr in dataTable.Rows)
        {
            Dictionary<string, object?> row = new (dataTable.Columns.Count);
            row.AddRange(dataTable.Columns.Cast<DataColumn>().Select(col =>
            {
                object value = dr[col];
                return new KeyValuePair<string, object?>(col.ColumnName, value == DBNull.Value ? null : value);
            }));
            result.Add(row);
        }

        return Task.FromResult<object?>(result.ToArray());
    }
}
