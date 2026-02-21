using CShells.Features;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature that configures MassTransit as the workflow dispatcher implementation.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Workflow Dispatcher",
    Description = "Uses MassTransit to dispatch workflows across the system",
    DependsOn = ["WorkflowRuntime", "MassTransit Service Bus"])]
[UsedImplicitly]
public class MassTransitWorkflowDispatcherShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<MassTransitWorkflowDispatcherOptions>(x => { });
        services.Configure<MassTransitStimulusDispatcherOptions>(x => { });
        
        services.AddScoped<MassTransitWorkflowCancellationDispatcher>();
        services.AddScoped<MassTransitStimulusDispatcher>();
        services.AddScoped<MassTransitWorkflowDispatcher>();
        services.AddScoped<ValidatingWorkflowDispatcher>();
        
        // Register as factory delegates that will be picked up by WorkflowRuntime
        services.AddSingleton<Func<IServiceProvider, IWorkflowDispatcher>>(sp =>
        {
            var decoratedService = sp.GetRequiredService<MassTransitWorkflowDispatcher>();
            return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, decoratedService);
        });
    }
}

