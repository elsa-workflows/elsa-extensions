using Elsa.FakeData.Activities;
using Elsa.FakeData.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;

namespace Elsa.FakeData.UnitTests.Activities;

public class GenerateFakeUsersTests
{
    [Fact]
    public async Task Execute_WithDefaultCount_Generates10Records()
    {
        // Arrange
        var result = new Output<FakeUser[]>();
        var activity = new GenerateFakeUsers { Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var users = context.Get(result);
        Assert.NotNull(users);
        Assert.Equal(10, users.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    public async Task Execute_WithCustomCount_GeneratesExpectedCount(int count)
    {
        // Arrange
        var result = new Output<FakeUser[]>();
        var activity = new GenerateFakeUsers { Count = new Input<int>(count), Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var users = context.Get(result);
        Assert.NotNull(users);
        Assert.Equal(count, users.Length);
    }

    [Fact]
    public async Task Execute_WithSeed_ProducesDeterministicData()
    {
        const int seed = 42;
        const int count = 5;

        // Arrange
        var result1 = new Output<FakeUser[]>();
        var activity1 = new GenerateFakeUsers
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result1,
        };

        var result2 = new Output<FakeUser[]>();
        var activity2 = new GenerateFakeUsers
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result2,
        };

        // Act
        var context1 = await new ActivityTestFixture(activity1).ExecuteAsync();
        var users1 = context1.Get(result1)!.ToList();

        var context2 = await new ActivityTestFixture(activity2).ExecuteAsync();
        var users2 = context2.Get(result2)!.ToList();

        // Assert
        Assert.Equal(users1.Count, users2.Count);
        for (var i = 0; i < users1.Count; i++)
        {
            Assert.Equal(users1[i].Id, users2[i].Id);
            Assert.Equal(users1[i].Username, users2[i].Username);
            Assert.Equal(users1[i].Email, users2[i].Email);
        }
    }

    [Fact]
    public async Task Execute_GeneratesUsers_WithAllPropertiesPopulated()
    {
        // Arrange
        var result = new Output<FakeUser[]>();
        var activity = new GenerateFakeUsers
        {
            Count = new Input<int>(5),
            Seed = new Input<int?>(1),
            Result = result,
        };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();
        var users = context.Get(result)!;

        // Assert
        foreach (var user in users)
        {
            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.False(string.IsNullOrWhiteSpace(user.Username));
            Assert.False(string.IsNullOrWhiteSpace(user.Email));
            Assert.False(string.IsNullOrWhiteSpace(user.FirstName));
            Assert.False(string.IsNullOrWhiteSpace(user.LastName));
            Assert.False(string.IsNullOrWhiteSpace(user.Avatar));
            Assert.NotNull(user.Roles);
            Assert.NotEmpty(user.Roles);
            Assert.NotEqual(default, user.CreatedAt);
        }
    }
}
