using System.Runtime.CompilerServices;
using Bogus;
using Elsa.FakeData.Extensions;
using Elsa.FakeData.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.FakeData.Activities;

/// <summary>
/// Generates a collection of fake <see cref="FakeProduct"/> records.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "Fake Data",
    DisplayName = "Generate Products",
    Description = "Generates a collection of fake product records.",
    Kind = ActivityKind.Task)]
public class GenerateFakeProducts : GenerateFakeDataActivity<FakeProduct>
{
    /// <inheritdoc />
    public GenerateFakeProducts([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override Faker<FakeProduct> CreateFaker(string locale, int? seed)
    {
        return new Faker<FakeProduct>(locale)
            .When(seed is not null, f => f.UseSeed(seed!.Value))
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => f.Finance.Amount(1, 1000))
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Department, f => f.Commerce.Department())
            .RuleFor(p => p.Rating, f => f.Random.Number(1, 5))
            .RuleFor(p => p.Stock, f => f.Random.Int(0, 1000));
    }
}
