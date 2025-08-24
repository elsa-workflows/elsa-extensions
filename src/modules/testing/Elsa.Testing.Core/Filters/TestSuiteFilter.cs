using Elsa.Testing.Core.Entities;

namespace Elsa.Testing.Core.Filters;

public class TestSuiteFilter
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public IQueryable<TestSuite> Apply(IQueryable<TestSuite> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (Name != null) queryable = queryable.Where(x => x.Name == Name);
        if (Description != null) queryable = queryable.Where(x => x.Description == Description);
        return queryable;
    }
}
