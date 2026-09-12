using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Workflows;

namespace Elsa.Dapper.UnitTests;

public class TryMarkInterruptedQueryTests
{
    [Fact(DisplayName = "Conditional interrupt update targets non-terminal rows only")]
    public void UpdateQuery_IncludesIdAndNonFinishedStatus()
    {
        var record = new
        {
            Id = "running-1",
            SubStatus = WorkflowSubStatus.Interrupted.ToString(),
            IsExecuting = false
        };

        var query = new ParameterizedQuery(new SqliteDialect())
            .Update("WorkflowInstances", record, ["SubStatus", "IsExecuting"])
            .Is("Id", record.Id)
            .IsNot("Status", WorkflowStatus.Finished.ToString());

        var sql = query.Sql.ToString();

        Assert.Contains("UPDATE WorkflowInstances SET SubStatus = @SubStatus, IsExecuting = @IsExecuting WHERE 1=1", sql, StringComparison.Ordinal);
        Assert.Contains("and Id = @Id", sql, StringComparison.Ordinal);
        Assert.Contains("and not Status = @Status", sql, StringComparison.Ordinal);
        Assert.Equal(WorkflowSubStatus.Interrupted.ToString(), query.Parameters.Get<string>("SubStatus"));
        Assert.False(query.Parameters.Get<bool>("IsExecuting"));
        Assert.Equal("running-1", query.Parameters.Get<string>("Id"));
        Assert.Equal(WorkflowStatus.Finished.ToString(), query.Parameters.Get<string>("Status"));
    }
}
