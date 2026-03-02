using Elsa.FakeData.Activities;
using Elsa.FakeData.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;

namespace Elsa.FakeData.UnitTests.Activities;

public class GenerateFakeOrdersTests
{
    [Fact]
    public async Task Execute_WithDefaultCount_Generates10Records()
    {
        // Arrange
        var result = new Output<FakeOrder[]>();
        var activity = new GenerateFakeOrders { Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var orders = context.Get(result);
        Assert.NotNull(orders);
        Assert.Equal(10, orders.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    public async Task Execute_WithCustomCount_GeneratesExpectedCount(int count)
    {
        // Arrange
        var result = new Output<FakeOrder[]>();
        var activity = new GenerateFakeOrders { Count = new Input<int>(count), Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var orders = context.Get(result);
        Assert.NotNull(orders);
        Assert.Equal(count, orders.Length);
    }

    [Fact]
    public async Task Execute_WithSeed_ProducesDeterministicData()
    {
        const int seed = 42;
        const int count = 5;

        // Arrange
        var result1 = new Output<FakeOrder[]>();
        var activity1 = new GenerateFakeOrders
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result1,
        };

        var result2 = new Output<FakeOrder[]>();
        var activity2 = new GenerateFakeOrders
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result2,
        };

        // Act
        var context1 = await new ActivityTestFixture(activity1).ExecuteAsync();
        var orders1 = context1.Get(result1)!.ToList();

        var context2 = await new ActivityTestFixture(activity2).ExecuteAsync();
        var orders2 = context2.Get(result2)!.ToList();

        // Assert
        Assert.Equal(orders1.Count, orders2.Count);
        for (var i = 0; i < orders1.Count; i++)
        {
            Assert.Equal(orders1[i].Id, orders2[i].Id);
            Assert.Equal(orders1[i].OrderNumber, orders2[i].OrderNumber);
            Assert.Equal(orders1[i].CustomerEmail, orders2[i].CustomerEmail);
        }
    }

    [Fact]
    public async Task Execute_GeneratesOrders_WithAllPropertiesPopulated()
    {
        // Arrange
        var result = new Output<FakeOrder[]>();
        var activity = new GenerateFakeOrders
        { 
            Count = new Input<int>(5),
            Seed = new Input<int?>(1),
            Result = result,
        };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();
        var orders = context.Get(result)!;

        // Assert
        foreach (var order in orders)
        {
            Assert.NotEqual(Guid.Empty, order.Id);
            Assert.False(string.IsNullOrWhiteSpace(order.OrderNumber));
            Assert.NotEqual(default, order.OrderDate);
            Assert.False(string.IsNullOrWhiteSpace(order.Status));
            Assert.False(string.IsNullOrWhiteSpace(order.CustomerName));
            Assert.False(string.IsNullOrWhiteSpace(order.CustomerEmail));
            Assert.False(string.IsNullOrWhiteSpace(order.ShippingAddress));
            Assert.True(order.SubTotal > 0);
            Assert.True(order.Tax >= 0);
            Assert.True(order.Total > 0);
        }
    }
}
