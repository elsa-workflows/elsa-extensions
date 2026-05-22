using Elsa.Sql.Handlers;
using Microsoft.Extensions.Logging;

namespace Elsa.Sql.Tests.Handlers;

public class SqlClientDataSetResultHandlerTests
{
    [Fact]
    public async Task Handle_DataSetPassedThrough()
    {
        var dataSet = new System.Data.DataSet();
        var table = dataSet.Tables.Add("TestTable");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(new object[] { 1, "Test" });
        table.Rows.Add(new object[] { 2, "Another Test" });

        var handler = new SqlClientDataSetResultHandler();
        var result = await handler.HandleAsync(dataSet, CancellationToken.None);
        Assert.Equal(dataSet, result);
    }

    [Fact]
    public async Task Handle_NullPassedThrough()
    {
        var handler = new SqlClientDataSetResultHandler();
        var result = await handler.HandleAsync(null, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NonDataSetException()
    {
        var handler = new SqlClientDataSetResultHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(new object(), CancellationToken.None));
    }
}