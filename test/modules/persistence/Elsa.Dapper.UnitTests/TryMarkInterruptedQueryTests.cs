using Elsa.Persistence.Dapper.Dialects;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Workflows;

namespace Elsa.Dapper.UnitTests;

public class TryMarkInterruptedQueryTests
{
    [Fact(DisplayName = "Conditional interrupt update allows Running or Finished/Cancelled")]
    public void UpdateQuery_AllowsRunningOrFinishedCancelled()
    {
        var record = new
        {
            Id = "running-1",
            Status = WorkflowStatus.Running.ToString(),
            SubStatus = WorkflowSubStatus.Interrupted.ToString(),
            IsExecuting = false
        };

        var query = new ParameterizedQuery(new SqliteDialect())
            .Update("WorkflowInstances", record, ["Status", "SubStatus", "IsExecuting"])
            .Is("Id", record.Id);
        query.Sql.AppendLine("and (not Status = @FinishedStatus or SubStatus = @CancelledSubStatus)");
        query.Parameters.Add("@FinishedStatus", WorkflowStatus.Finished.ToString());
        query.Parameters.Add("@CancelledSubStatus", WorkflowSubStatus.Cancelled.ToString());

        var sql = query.Sql.ToString();

        Assert.Contains("UPDATE WorkflowInstances SET Status = @Status, SubStatus = @SubStatus, IsExecuting = @IsExecuting WHERE 1=1", sql, StringComparison.Ordinal);
        Assert.Contains("and Id = @Id", sql, StringComparison.Ordinal);
        Assert.Contains("and (not Status = @FinishedStatus or SubStatus = @CancelledSubStatus)", sql, StringComparison.Ordinal);
        Assert.Equal(WorkflowStatus.Running.ToString(), query.Parameters.Get<string>("Status"));
        Assert.Equal(WorkflowSubStatus.Interrupted.ToString(), query.Parameters.Get<string>("SubStatus"));
        Assert.False(query.Parameters.Get<bool>("IsExecuting"));
        Assert.Equal("running-1", query.Parameters.Get<string>("Id"));
        Assert.Equal(WorkflowStatus.Finished.ToString(), query.Parameters.Get<string>("FinishedStatus"));
        Assert.Equal(WorkflowSubStatus.Cancelled.ToString(), query.Parameters.Get<string>("CancelledSubStatus"));
    }
}
