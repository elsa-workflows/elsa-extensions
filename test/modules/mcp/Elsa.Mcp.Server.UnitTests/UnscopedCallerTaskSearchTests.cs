using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Mcp.Server.UnitTests;

public class UnscopedCallerTaskSearchTests
{
    [Fact]
    public async Task PassesCriteriaToStoreAsPlainFilter()
    {
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;
        PageArgs? capturedPageArgs = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call =>
            {
                captured = call.Arg<WorkflowInstanceFilter>();
                capturedPageArgs = call.Arg<PageArgs>();
            });

        UnscopedCallerTaskSearch search = new(store);
        CallerTaskCriteria criteria = new("needle", WorkflowStatus.Running, [WorkflowSubStatus.Suspended]);

        _ = await search.FindAsync(criteria, new PageArgs { Offset = 5, Limit = 10 }, cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("needle", captured.SearchTerm);
        Assert.Equal(WorkflowStatus.Running, captured.WorkflowStatus);
        Assert.Equal([WorkflowSubStatus.Suspended], captured.WorkflowSubStatuses);
        Assert.Equal(5, capturedPageArgs!.Offset);
        Assert.Equal(10, capturedPageArgs.Limit);
    }

    [Fact]
    public async Task ReturnsWhatTheStoreReturns()
    {
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceSummary summary = new() { Id = "instance-1" };

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([summary], 1));

        UnscopedCallerTaskSearch search = new(store);

        Page<WorkflowInstanceSummary> page = await search.FindAsync(
            new CallerTaskCriteria(null, null, null),
            new PageArgs(),
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("instance-1", Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task LeavesTheSystemInstancesOffTheFilterByDefault()
    {
        // A system workflow is the host's own housekeeping. It is not one of the caller's tasks, so the tools that
        // answer "my tasks" leave it out unless the criteria ask for it.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call => captured = call.Arg<WorkflowInstanceFilter>());

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(new CallerTaskCriteria(null, null, null), new PageArgs(), cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.False(captured.IsSystem);
    }

    [Fact]
    public async Task FiltersOnNothingWhenTheSystemInstancesAreAskedFor()
    {
        // Null rather than true: including them widens the search to every instance, it does not narrow it to the
        // system ones.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call => captured = call.Arg<WorkflowInstanceFilter>());

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(
            new CallerTaskCriteria(null, null, null, IncludeSystem: true),
            new PageArgs(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured.IsSystem);
    }

    [Fact]
    public async Task LeavesAnEmptySubStatusSetOffTheFilter()
    {
        // Elsa's Apply tests WorkflowSubStatuses for null, not for emptiness: handing it an empty collection filters
        // every instance away, which is the opposite of what "no status filter" means.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call => captured = call.Arg<WorkflowInstanceFilter>());

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(new CallerTaskCriteria(null, null, []), new PageArgs(), cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured.WorkflowSubStatuses);
    }

    [Fact]
    public async Task PassesTheDefinitionIdsToTheStore()
    {
        // The definition filter has to narrow the query rather than the page that came back. Filtering after the fact
        // leaves the store counting instances the caller never asked for, so the pager reports a total that does not
        // match the rows under it.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call => captured = call.Arg<WorkflowInstanceFilter>());

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(
            new CallerTaskCriteria(null, null, null, DefinitionIds: ["alpha", "beta"]),
            new PageArgs(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(["alpha", "beta"], captured.DefinitionIds);
    }

    [Fact]
    public async Task AsksTheStoreForTheOrderTheCallerGave()
    {
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<WorkflowInstanceOrder<DateTimeOffset>>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0));

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(
            new CallerTaskCriteria(null, null, null),
            new PageArgs(),
            new CallerTaskOrder(CallerTaskOrderField.UpdatedAt, OrderDirection.Descending),
            CancellationToken.None);

        // The typed overload, and with the order the caller asked for: the untyped one silently orders by creation
        // date, so a search that called it would answer a page that looks sorted and is not.
        _ = store.Received(1).SummarizeManyAsync(
                Arg.Any<WorkflowInstanceFilter>(),
                Arg.Any<PageArgs>(),
                Arg.Is<WorkflowInstanceOrder<DateTimeOffset>>(order => order.Direction == OrderDirection.Descending),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeavesTheOrderToTheStoreWhenTheCallerGivesNone()
    {
        // The store's own overload rather than this search spelling out what that overload does. They agree today, and
        // asserting on the call is what keeps the claim in the code honest if one of them ever changes.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0));

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(
            new CallerTaskCriteria(null, null, null),
            new PageArgs(),
            cancellationToken: CancellationToken.None);

        _ = store.Received(1).SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeavesAnEmptyDefinitionSetOffTheFilter()
    {
        // Elsa's Apply tests DefinitionIds for null, not for emptiness, so an empty collection filters every instance
        // away. A cleared definition picker means "any definition", which is the opposite.
        IWorkflowInstanceStore store = Substitute.For<IWorkflowInstanceStore>();
        WorkflowInstanceFilter? captured = null;

        _ = store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<PageArgs>(), Arg.Any<CancellationToken>())
            .Returns(new Page<WorkflowInstanceSummary>([], 0))
            .AndDoes(call => captured = call.Arg<WorkflowInstanceFilter>());

        UnscopedCallerTaskSearch search = new(store);

        _ = await search.FindAsync(
            new CallerTaskCriteria(null, null, null, DefinitionIds: []),
            new PageArgs(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Null(captured.DefinitionIds);
    }
}
