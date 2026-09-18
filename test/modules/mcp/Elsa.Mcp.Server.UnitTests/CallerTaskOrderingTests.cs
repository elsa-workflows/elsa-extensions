using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Mcp.Server.UnitTests;

public class CallerTaskOrderingTests
{
    [Theory]
    [InlineData(CallerTaskOrderField.CreatedAt, nameof(WorkflowInstance.CreatedAt))]
    [InlineData(CallerTaskOrderField.UpdatedAt, nameof(WorkflowInstance.UpdatedAt))]
    [InlineData(CallerTaskOrderField.FinishedAt, nameof(WorkflowInstance.FinishedAt))]
    [InlineData(CallerTaskOrderField.Name, nameof(WorkflowInstance.Name))]
    [InlineData(CallerTaskOrderField.Status, nameof(WorkflowInstance.Status))]
    [InlineData(CallerTaskOrderField.SubStatus, nameof(WorkflowInstance.SubStatus))]
    public async Task OrdersOnTheInstanceColumnTheFieldNames(CallerTaskOrderField field, string expectedColumn)
    {
        RecordingSummarizer summarizer = new();

        _ = await CallerTaskOrdering.SummarizeAsync(new CallerTaskOrder(field), summarizer, CancellationToken.None);

        Assert.Equal(expectedColumn, summarizer.Column);
    }

    [Fact]
    public async Task FallsBackToTheCreationDateForAFieldWithNoCase()
    {
        // A member added to the enum without a case in the switch. It has to degrade to the column both instance
        // stores order by when no order is given at all, because this is a read path: a task list that throws is worse
        // than one sorted on the wrong column, and the fallback is the order the list had before anyone asked to sort.
        RecordingSummarizer summarizer = new();

        _ = await CallerTaskOrdering.SummarizeAsync(new CallerTaskOrder((CallerTaskOrderField)999), summarizer, CancellationToken.None);

        Assert.Equal(nameof(WorkflowInstance.CreatedAt), summarizer.Column);
    }

    [Theory]
    [InlineData(OrderDirection.Ascending)]
    [InlineData(OrderDirection.Descending)]
    public async Task PassesTheDirectionOnUnchanged(OrderDirection direction)
    {
        RecordingSummarizer summarizer = new();

        _ = await CallerTaskOrdering.SummarizeAsync(
            new CallerTaskOrder(CallerTaskOrderField.UpdatedAt, direction),
            summarizer,
            CancellationToken.None);

        Assert.Equal(direction, summarizer.Direction);
    }

    [Fact]
    public async Task OrdersTheFinishedDateAsANullableSoAnUnfinishedTaskStillSorts()
    {
        // FinishedAt is null while a task runs. Typing the order as a non-nullable DateTimeOffset would compile - the
        // key selector would just carry a conversion - and turn every unfinished row into a default date at one end of
        // the order rather than a null the database sorts as one.
        RecordingSummarizer summarizer = new();

        _ = await CallerTaskOrdering.SummarizeAsync(
            new CallerTaskOrder(CallerTaskOrderField.FinishedAt),
            summarizer,
            CancellationToken.None);

        Assert.Equal(typeof(DateTimeOffset?), summarizer.KeyType);
    }

    [Fact]
    public async Task ReturnsWhatTheSummarizerReturns()
    {
        RecordingSummarizer summarizer = new(new Page<WorkflowInstanceSummary>([new WorkflowInstanceSummary { Id = "instance-1" }], 1));

        Page<WorkflowInstanceSummary> page = await CallerTaskOrdering.SummarizeAsync(
            new CallerTaskOrder(CallerTaskOrderField.Name),
            summarizer,
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("instance-1", Assert.Single(page.Items).Id);
    }

    /// <summary>
    /// Records the order it is handed instead of querying anything.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than faked: the method under test is generic in the order's key type, and what these tests
    /// assert on is that type together with the property the key selector reaches for. A fake would capture the
    /// argument but not the type argument it arrived under.
    /// </remarks>
    private sealed class RecordingSummarizer(Page<WorkflowInstanceSummary>? result = null) : ICallerTaskSummarizer
    {
        /// <summary>The instance property the order reads, by name.</summary>
        public string? Column { get; private set; }

        /// <summary>The direction the order carried.</summary>
        public OrderDirection Direction { get; private set; }

        /// <summary>The type the order was generic in.</summary>
        public Type? KeyType { get; private set; }

        /// <inheritdoc />
        public ValueTask<Page<WorkflowInstanceSummary>> SummarizeAsync<TOrderBy>(WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken)
        {
            // Off the expression rather than off anything the order says about itself: which column a field means is
            // the one thing under test here, and only the key selector states it.
            Column = order.KeySelector.Body is MemberExpression member ? member.Member.Name : order.KeySelector.Body.ToString();
            Direction = order.Direction;
            KeyType = typeof(TOrderBy);

            return new ValueTask<Page<WorkflowInstanceSummary>>(result ?? new Page<WorkflowInstanceSummary>([], 0));
        }
    }
}
