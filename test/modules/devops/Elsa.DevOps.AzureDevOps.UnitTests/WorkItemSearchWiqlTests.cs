using Elsa.DevOps.AzureDevOps.Services;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class WorkItemSearchWiqlTests
{
    [Fact]
    public void SelectsEverythingInTheProjectWhenNoFilterIsGiven()
    {
        string wiql = WorkItemSearchWiql.Build(null, null, null, null, null);

        Assert.Contains("[System.TeamProject] = @project", wiql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY [System.ChangedDate] DESC", wiql, StringComparison.Ordinal);
        Assert.DoesNotContain("CONTAINS", wiql, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchesTheTitleForFreeText()
    {
        Assert.Contains("[System.Title] CONTAINS 'inloggen'", WorkItemSearchWiql.Build("inloggen", null, null, null, null), StringComparison.Ordinal);
    }

    [Fact]
    public void MatchesTypeAndStateWhole()
    {
        string wiql = WorkItemSearchWiql.Build(null, "Bug", "Active", null, null);

        Assert.Contains("[System.WorkItemType] = 'Bug'", wiql, StringComparison.Ordinal);
        Assert.Contains("[System.State] = 'Active'", wiql, StringComparison.Ordinal);
    }

    [Fact]
    public void NarrowsOnATagWithContainsBecauseThatIsAllWiqlCanDo()
    {
        // The whole-tag comparison happens afterwards, in the tool. WIQL's CONTAINS is a substring test over the
        // semicolon-separated list, so this query returns candidates rather than matches.
        Assert.Contains("[System.Tags] CONTAINS 'problemId:19'", WorkItemSearchWiql.Build(null, null, null, "problemId:19", null), StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesTheCallerWithTheMeMacroRatherThanANameItWouldHaveToGuess()
    {
        // @me is whoever the token belongs to, which is the user the workflow runs for. Quoting it would search for a
        // person literally called "@me".
        Assert.Contains("[System.AssignedTo] = @me", WorkItemSearchWiql.Build(null, null, null, null, "me"), StringComparison.Ordinal);
        Assert.Contains("[System.AssignedTo] = @me", WorkItemSearchWiql.Build(null, null, null, null, "@me"), StringComparison.Ordinal);
    }

    [Fact]
    public void MatchesANamedAssignee()
    {
        Assert.Contains("[System.AssignedTo] = 'alice@contoso.com'", WorkItemSearchWiql.Build(null, null, null, null, "alice@contoso.com"), StringComparison.Ordinal);
    }

    [Fact]
    public void EscapesAQuoteRatherThanLettingItCloseTheLiteral()
    {
        // A title with an apostrophe is ordinary - "Can't log in" - and would otherwise produce WIQL that does not parse,
        // or worse, parses into something else.
        Assert.Contains("CONTAINS 'Can''t log in'", WorkItemSearchWiql.Build("Can't log in", null, null, null, null), StringComparison.Ordinal);
    }

    [Fact]
    public void IgnoresAFilterThatIsBlankRatherThanSearchingForNothing()
    {
        string wiql = WorkItemSearchWiql.Build("   ", "", null, "  ", null);

        Assert.DoesNotContain("CONTAINS", wiql, StringComparison.Ordinal);
        Assert.DoesNotContain("System.WorkItemType", wiql, StringComparison.Ordinal);
    }
}
