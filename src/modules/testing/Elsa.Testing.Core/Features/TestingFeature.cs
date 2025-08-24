using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Testing.Core.Contracts;
using Elsa.Testing.Core.Options;
using Elsa.Testing.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Core.Features;

public class TestingFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, ITestScenarioStore> _testScenarioStoreFactory = sp => sp.GetRequiredService<MemoryTestScenarioStore>();
    private Func<IServiceProvider, ITestRunStore> _testRunStoreFactory = sp => sp.GetRequiredService<MemoryTestRunStore>();
    private Func<IServiceProvider, ITestSuiteStore> _testSuiteStoreFactory = sp => sp.GetRequiredService<MemoryTestSuiteStore>();
    private Func<IServiceProvider, ITestSuiteRunStore> _testSuiteRunStoreFactory = sp => sp.GetRequiredService<MemoryTestSuiteRunStore>();

    public TestingFeature UseTestScenarioStore(Func<IServiceProvider, ITestScenarioStore> factory)
    {
        _testScenarioStoreFactory = factory;
        return this;
    }

    public TestingFeature UseTestRunStore(Func<IServiceProvider, ITestRunStore> factory)
    {
        _testRunStoreFactory = factory;
        return this;
    }

    public TestingFeature UseTestSuiteStore(Func<IServiceProvider, ITestSuiteStore> factory)
    {
        _testSuiteStoreFactory = factory;
        return this;
    }

    public TestingFeature UseTestSuiteRunStore(Func<IServiceProvider, ITestSuiteRunStore> factory)
    {
        _testSuiteRunStoreFactory = factory;
        return this;
    }

    public TestingFeature ConfigureOptions(Action<TestingOptions> configureOptions)
    {
        Services.Configure(configureOptions);
        return this;
    }

    public override void Configure()
    {
    }

    public override void Apply()
    {
        Services
            .AddScoped(_testScenarioStoreFactory)
            .AddScoped(_testRunStoreFactory)
            .AddScoped(_testSuiteStoreFactory)
            .AddScoped(_testSuiteRunStoreFactory)
            ;
    }
}