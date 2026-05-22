using Elsa.Sql.Handlers;
using Microsoft.Extensions.Logging;

namespace Elsa.Sql.Tests.Handlers;

public class SqlClientRecordSetResultHandlerTests
{
    [Fact]
    public async Task Handle_DataSetAsArrayOfDictionaryObjects()
    {
        var dataSet = new System.Data.DataSet();
        var table = dataSet.Tables.Add("TestTable");
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(new object[] { 1, "Test" });
        table.Rows.Add(new object[] { 2, "Another Test" });

        var handler = new SqlClientRecordSetResultHandler();
        var result = await handler.HandleAsync(dataSet, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, object?>[]>(result);
        Assert.Equal(2, ((Dictionary<string, object?>[])result).Length);
        Assert.Equal(1, ((Dictionary<string, object?>[])result)[0]["Id"]);
        Assert.Equal("Test", ((Dictionary<string, object?>[])result)[0]["Name"]);
        Assert.Equal(2, ((Dictionary<string, object?>[])result)[1]["Id"]);
        Assert.Equal("Another Test", ((Dictionary<string, object?>[])result)[1]["Name"]);
    }

    [Fact]
    public async Task Handle_NullPassedThrough()
    {
        var handler = new SqlClientRecordSetResultHandler();
        var result = await handler.HandleAsync(null, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NonDataSetException()
    {
        var handler = new SqlClientRecordSetResultHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(new object(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoTables() 
    {
        var dataSet = new System.Data.DataSet();
        var handler = new SqlClientRecordSetResultHandler();
        var result = await handler.HandleAsync(dataSet, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_MoreThanOneTable()
    {
        var dataSet = new System.Data.DataSet();
        dataSet.Tables.Add("Table1");
        dataSet.Tables.Add("Table2");
        var handler = new SqlClientRecordSetResultHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(dataSet, CancellationToken.None));
    }
}