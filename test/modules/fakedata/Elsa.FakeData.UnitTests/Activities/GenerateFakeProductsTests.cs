using Elsa.FakeData.Activities;
using Elsa.FakeData.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;

namespace Elsa.FakeData.UnitTests.Activities;

public class GenerateFakeProductsTests
{
    [Fact]
    public async Task Execute_WithDefaultCount_Generates10Records()
    {
        // Arrange
        var result = new Output<ICollection<FakeProduct>>();
        var activity = new GenerateFakeProducts { Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var products = context.Get(result);
        Assert.NotNull(products);
        Assert.Equal(10, products.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    public async Task Execute_WithCustomCount_GeneratesExpectedCount(int count)
    {
        // Arrange
        var result = new Output<ICollection<FakeProduct>>();
        var activity = new GenerateFakeProducts { Count = new Input<int>(count), Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var products = context.Get(result);
        Assert.NotNull(products);
        Assert.Equal(count, products.Count);
    }

    [Fact]
    public async Task Execute_WithSeed_ProducesDeterministicData()
    {
        const int seed = 42;
        const int count = 5;

        // Arrange
        var result1 = new Output<ICollection<FakeProduct>>();
        var activity1 = new GenerateFakeProducts
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result1,
        };

        var result2 = new Output<ICollection<FakeProduct>>();
        var activity2 = new GenerateFakeProducts
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result2,
        };

        // Act
        var context1 = await new ActivityTestFixture(activity1).ExecuteAsync();
        var products1 = context1.Get(result1)!.ToList();

        var context2 = await new ActivityTestFixture(activity2).ExecuteAsync();
        var products2 = context2.Get(result2)!.ToList();

        // Assert
        Assert.Equal(products1.Count, products2.Count);
        for (var i = 0; i < products1.Count; i++)
        {
            Assert.Equal(products1[i].Id, products2[i].Id);
            Assert.Equal(products1[i].Name, products2[i].Name);
            Assert.Equal(products1[i].Price, products2[i].Price);
        }
    }

    [Fact]
    public async Task Execute_GeneratesProducts_WithAllPropertiesPopulated()
    {
        // Arrange
        var result = new Output<ICollection<FakeProduct>>();
        var activity = new GenerateFakeProducts
        {
            Count = new Input<int>(5),
            Seed = new Input<int?>(1),
            Result = result,
        };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();
        var products = context.Get(result)!;

        // Assert
        foreach (var product in products)
        {
            Assert.NotEqual(Guid.Empty, product.Id);
            Assert.False(string.IsNullOrWhiteSpace(product.Name));
            Assert.False(string.IsNullOrWhiteSpace(product.Description));
            Assert.True(product.Price > 0);
            Assert.False(string.IsNullOrWhiteSpace(product.Category));
            Assert.False(string.IsNullOrWhiteSpace(product.Department));
            Assert.InRange(product.Rating, 1, 5);
            Assert.True(product.Stock >= 0);
        }
    }
}
