using System.ComponentModel;
using System.Reflection;
using CShells.Features;
using Elsa.Common;
using Elsa.ServiceBus.MassTransit.Contracts;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Formatters;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature for enabling the MassTransit service bus.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Service Bus",
    Description = "Enables MassTransit-based message publishing and handling for workflows")]
[UsedImplicitly]
public class MassTransitShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        var messageTypes = CollectMessageTypes();

        services.AddSingleton<IEndpointChannelFormatter>(DefaultEndpointChannelFormatter);
        services.Configure<MassTransitOptions>(x => x.PrefetchCount ??= null);
        services.Configure<MassTransitWorkflowDispatcherOptions>(x => { });
        services.AddActivityProvider<MassTransitActivityTypeProvider>();
        
        // Add MassTransit with in-memory transport by default
        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            ConfigureInMemoryTransport(bus);
        });

        services.AddOptions<MassTransitHostOptions>().Configure(options =>
        {
            // Wait until the bus is started before returning from IHostedService.StartAsync.
            options.WaitUntilStarted = true;
        });

        // Add collected message types to options.
        services.Configure<MassTransitActivityOptions>(options => options.MessageTypes = new HashSet<Type>(messageTypes));

        // Add collected message types as available variable types.
        services.Configure<ManagementOptions>(options =>
        {
            foreach (var messageType in messageTypes)
            {
                var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
                var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
                var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
                var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
                var description = descriptionAttr?.Description ?? activityAttr?.Description;
                options.VariableDescriptors.Add(new(messageType, category, description));
            }
        });
    }

    private IEndpointChannelFormatter DefaultEndpointChannelFormatter(IServiceProvider sp) => new DefaultEndpointChannelFormatter();

    private IEnumerable<Type> CollectMessageTypes()
    {
        // This would need to be implemented based on your message discovery mechanism
        // For now, return empty set - can be extended based on specific needs
        return Enumerable.Empty<Type>();
    }

    private void ConfigureInMemoryTransport(IBusRegistrationConfigurator configure)
    {
        configure.UsingInMemory((context, bus) =>
        {
            var options = context.GetRequiredService<IOptions<MassTransitWorkflowDispatcherOptions>>().Value;
            var busOptions = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;

            bus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));

            bus.ConfigureJsonSerializerOptions(serializerOptions =>
            {
                var serializer = context.GetRequiredService<IJsonSerializer>();
                serializer.ApplyOptions(serializerOptions);
                return serializerOptions;
            });

            if (busOptions.PrefetchCount.HasValue) 
                bus.PrefetchCount = busOptions.PrefetchCount.Value;

            bus.ConfigureTenantMiddleware(context);
        });
    }
}

