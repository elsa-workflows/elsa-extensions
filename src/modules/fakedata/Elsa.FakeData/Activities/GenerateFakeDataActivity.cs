using Bogus;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.FakeData.Activities;

/// <summary>
/// Provides a base implementation for activities that generate collections of fake data
/// using the <see href="https://github.com/bchavez/Bogus">Bogus</see> library.
/// </summary>
public abstract class GenerateFakeDataActivity<T> : CodeActivity<T[]> where T : class
{
    /// <inheritdoc />
    protected GenerateFakeDataActivity(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The number of fake records to generate.
    /// </summary>
    [Input(
        DisplayName = "Number of records",
        Description = "The number of fake records to generate. Default: 10")]
    public Input<int> Count { get; set; } = new(10);

    /// <summary>
    /// The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl').
    /// See the <see href="https://github.com/bchavez/Bogus#locales">Bogus locale list</see> for all supported values.
    /// </summary>
    [Input(
        DisplayName = "Locale",
        Description = "The locale to use when generating fake data (e.g. 'en', 'de', 'fr', 'nl'). Default: en")]
    public Input<string> Locale { get; set; } = new("en");

    /// <summary>
    /// A seed value for the random number generator.
    /// Providing a seed will ensure that the same fake data is generated on each run, which can be useful for deterministic testing.
    /// If not provided, a random seed will be used, resulting in different data on each execution.
    /// </summary>
    [Input(
        DisplayName = "Seed",
        Description = "With a seed, each run will produce the identical data (for deterministic tests).")]
    public Input<int?> Seed { get; set; } = new((int?)null);

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var count = Count.Get(context);
        var locale = Locale.GetOrDefault(context) ?? "en";
        var seed = Seed.GetOrDefault(context, () => null);

        var faker = CreateFaker(locale, seed);
        var items = faker.Generate(count);

        context.Set(Result, items.ToArray());

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a configured <see cref="Faker{T}"/> instance for the given locale.
    /// </summary>
    /// <param name="locale">The locale string (e.g. "en", "de", "fr").</param>
    /// <param name="seed">An optional seed value for deterministic data generation.</param>
    /// <returns>A configured <see cref="Faker{T}"/> instance.</returns>
    protected abstract Faker<T> CreateFaker(string locale, int? seed);
}
