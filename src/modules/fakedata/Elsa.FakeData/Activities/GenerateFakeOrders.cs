using System.Globalization;
using System.Runtime.CompilerServices;
using Bogus;
using Elsa.FakeData.Extensions;
using Elsa.FakeData.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.FakeData.Activities;

/// <summary>
/// Generates a collection of fake <see cref="FakeOrder"/> records.
/// </summary>
[Activity(
    Namespace = "Elsa",
    Category = "Fake Data",
    DisplayName = "Generate Orders",
    Description = "Generates a collection of fake order records.",
    Kind = ActivityKind.Task)]
public class GenerateFakeOrders : GenerateFakeDataActivity<FakeOrder>
{
    /// <inheritdoc />
    public GenerateFakeOrders([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override Faker<FakeOrder> CreateFaker(string locale, int? seed)
    {
        return new Faker<FakeOrder>(locale)
            .When(seed is not null, f => f.UseSeed(seed!.Value))
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.OrderNumber, f => f.Random.Number(100_000, 1_000_000_000).ToString(CultureInfo.InvariantCulture))
            .RuleFor(o => o.OrderDate, f => f.Date.Past())
            .RuleFor(o => o.Status, f => f.PickRandom("Pending", "Processing", "Shipped", "Delivered", "Cancelled"))
            .RuleFor(o => o.CustomerName, f => f.Person.FullName)
            .RuleFor(o => o.CustomerEmail, f => f.Person.Email)
            .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
            .RuleFor(o => o.SubTotal, f => f.Finance.Amount(10, 500))
            .RuleFor(o => o.Tax, (_, o) => Math.Round(o.SubTotal * 0.1m, 2))
            .RuleFor(o => o.Total, (_, o) => o.SubTotal + o.Tax);
    }
}
