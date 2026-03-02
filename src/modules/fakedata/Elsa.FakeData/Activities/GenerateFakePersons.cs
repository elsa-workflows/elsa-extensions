using System.Runtime.CompilerServices;
using Bogus;
using Elsa.FakeData.Extensions;
using Elsa.FakeData.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.FakeData.Activities;

/// <summary>
/// Generates a collection of fake <see cref="FakePerson"/> records.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "Fake Data",
    DisplayName = "Generate Persons",
    Description = "Generates a collection of fake person records.",
    Kind = ActivityKind.Task)]
public class GenerateFakePersons : GenerateFakeDataActivity<FakePerson>
{
    /// <inheritdoc />
    public GenerateFakePersons([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override Faker<FakePerson> CreateFaker(string locale, int? seed)
    {
        return new Faker<FakePerson>(locale)
            .When(seed is not null, f => f.UseSeed(seed!.Value))
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.FirstName, f => f.Person.FirstName)
            .RuleFor(p => p.LastName, f => f.Person.LastName)
            .RuleFor(p => p.FullName, (f) => f.Person.FullName)
            .RuleFor(p => p.Email, (f, p) => f.Person.Email)
            .RuleFor(p => p.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(80, DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.Gender, f => f.PickRandom("Male", "Female", "Non-binary"))
            .RuleFor(p => p.Address, f => f.Address.StreetAddress())
            .RuleFor(p => p.City, f => f.Address.City())
            .RuleFor(p => p.State, f => f.Address.State())
            .RuleFor(p => p.ZipCode, f => f.Address.ZipCode())
            .RuleFor(p => p.Country, f => f.Address.Country());
    }
}
