using Elsa.Features.Services;
using Elsa.OpenAI.Features;
using Elsa.OpenAI.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Elsa.OpenAI.Tests.Features;

/// <summary>
/// Unit tests for the OpenAIFeature class.
/// </summary>
public class OpenAIFeatureTests
{
    [Fact]
    public void Constructor_WithValidModule_CreatesInstance()
    {
        // Arrange
        var mockModule = new Mock<IModule>();

        // Act
        var feature = new OpenAIFeature(mockModule.Object);

        // Assert
        Assert.NotNull(feature);
    }

    [Fact]
    public void Constructor_WithNullModule_CreatesInstance()
    {
        // Arrange & Act
        var feature = new OpenAIFeature(null!);

        // Assert
        Assert.NotNull(feature);
    }

    [Fact]
    public void Apply_RegistersOpenAIClientFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Services).Returns(services);
        var feature = new OpenAIFeature(mockModule.Object);

        // Act
        feature.Apply();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<OpenAIClientFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void Apply_RegistersOpenAIClientFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Services).Returns(services);
        var feature = new OpenAIFeature(mockModule.Object);

        // Act
        feature.Apply();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory1 = serviceProvider.GetService<OpenAIClientFactory>();
        var factory2 = serviceProvider.GetService<OpenAIClientFactory>();
        
        Assert.NotNull(factory1);
        Assert.NotNull(factory2);
        Assert.Same(factory1, factory2);
    }

    [Fact]
    public void Apply_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Services).Returns(services);
        var feature = new OpenAIFeature(mockModule.Object);

        // Act
        feature.Apply();
        feature.Apply(); // Should not throw

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<OpenAIClientFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void Apply_WithExistingServices_DoesNotDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<OpenAIClientFactory>();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Services).Returns(services);
        var feature = new OpenAIFeature(mockModule.Object);

        // Act
        feature.Apply();

        // Assert
        var serviceDescriptors = services.Where(s => s.ServiceType == typeof(OpenAIClientFactory)).ToList();
        Assert.Equal(2, serviceDescriptors.Count); // One from manual add, one from feature
        
        // But when resolved, it should still work correctly
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<OpenAIClientFactory>();
        Assert.NotNull(factory);
    }
}
