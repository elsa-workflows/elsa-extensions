using System.Runtime.CompilerServices;
using Bogus;
using Elsa.FakeData.Extensions;
using Elsa.FakeData.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.FakeData.Activities;

/// <summary>
/// Generates a collection of fake <see cref="FakeUser"/> records.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "Fake Data",
    DisplayName = "Generate Users",
    Description = "Generates a collection of fake user records.",
    Kind = ActivityKind.Task)]
public class GenerateFakeUsers : GenerateFakeDataActivity<FakeUser>
{
    /// <inheritdoc />
    public GenerateFakeUsers([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override Faker<FakeUser> CreateFaker(string locale, int? seed)
    {
        return new Faker<FakeUser>(locale)
            .When(seed is not null, f => f.UseSeed(seed!.Value))
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Person.FirstName)
            .RuleFor(u => u.LastName, f => f.Person.LastName)
            .RuleFor(u => u.Username, (f, u) => f.Person.UserName)
            .RuleFor(u => u.Email, (f, u) => f.Person.Email)
            .RuleFor(u => u.Avatar, f => f.Person.Avatar)
            .RuleFor(u => u.Roles, f => [f.PickRandom("Admin", "User", "Moderator", "Guest")])
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(5));
    }
}
