using Elsa.FakeData.Activities;
using Elsa.FakeData.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;

namespace Elsa.FakeData.UnitTests.Activities;

public class GenerateFakePersonsTests
{
    [Fact]
    public async Task Execute_WithDefaultCount_Generates10Records()
    {
        // Arrange
        var result = new Output<ICollection<FakePerson>>();
        var activity = new GenerateFakePersons { Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var persons = context.Get(result);
        Assert.NotNull(persons);
        Assert.Equal(10, persons.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(25)]
    public async Task Execute_WithCustomCount_GeneratesExpectedCount(int count)
    {
        // Arrange
        var result = new Output<ICollection<FakePerson>>();
        var activity = new GenerateFakePersons { Count = new Input<int>(count), Result = result };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();

        // Assert
        var persons = context.Get(result);
        Assert.NotNull(persons);
        Assert.Equal(count, persons.Count);
    }

    [Fact]
    public async Task Execute_WithSeed_ProducesDeterministicData()
    {
        const int seed = 42;
        const int count = 5;

        // Arrange
        var result1 = new Output<ICollection<FakePerson>>();
        var activity1 = new GenerateFakePersons
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result1,
        };

        var result2 = new Output<ICollection<FakePerson>>();
        var activity2 = new GenerateFakePersons
        {
            Count = new Input<int>(count),
            Seed = new Input<int?>(seed),
            Result = result2,
        };

        // Act
        var context1 = await new ActivityTestFixture(activity1).ExecuteAsync();
        var persons1 = context1.Get(result1)!.ToList();

        var context2 = await new ActivityTestFixture(activity2).ExecuteAsync();
        var persons2 = context2.Get(result2)!.ToList();

        // Assert
        Assert.Equal(persons1.Count, persons2.Count);
        for (var i = 0; i < persons1.Count; i++)
        {
            Assert.Equal(persons1[i].Id, persons2[i].Id);
            Assert.Equal(persons1[i].FullName, persons2[i].FullName);
            Assert.Equal(persons1[i].Email, persons2[i].Email);
        }
    }

    [Fact]
    public async Task Execute_GeneratesPersons_WithAllPropertiesPopulated()
    {
        // Arrange
        var result = new Output<ICollection<FakePerson>>();
        var activity = new GenerateFakePersons
        {
            Count = new Input<int>(5),
            Seed = new Input<int?>(1),
            Result = result,
        };

        // Act
        var context = await new ActivityTestFixture(activity).ExecuteAsync();
        var persons = context.Get(result)!;

        // Assert
        foreach (var person in persons)
        {
            Assert.NotEqual(Guid.Empty, person.Id);
            Assert.False(string.IsNullOrWhiteSpace(person.FirstName));
            Assert.False(string.IsNullOrWhiteSpace(person.LastName));
            Assert.False(string.IsNullOrWhiteSpace(person.FullName));
            Assert.False(string.IsNullOrWhiteSpace(person.Email));
            Assert.False(string.IsNullOrWhiteSpace(person.Phone));
            Assert.NotEqual(default, person.DateOfBirth);
            Assert.False(string.IsNullOrWhiteSpace(person.Gender));
            Assert.False(string.IsNullOrWhiteSpace(person.Address));
            Assert.False(string.IsNullOrWhiteSpace(person.City));
            Assert.False(string.IsNullOrWhiteSpace(person.State));
            Assert.False(string.IsNullOrWhiteSpace(person.ZipCode));
            Assert.False(string.IsNullOrWhiteSpace(person.Country));
        }
    }
}
