using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginProvider<T>(this IServiceCollection services) where T: class, IPluginProvider
    {
        return services.AddScoped<IPluginProvider, T>();
    }
    
    public static IServiceCollection AddAgentServiceProvider<T>(this IServiceCollection services) where T: class, IAgentServiceProvider
    {
        return services.AddScoped<IAgentServiceProvider, T>();
    }
    
    /// <summary>
    /// Registers a code-first agent definition.
    /// </summary>
    public static IServiceCollection AddAgentDefinition<T>(this IServiceCollection services) where T : class, IAgentDefinition
    {
        return services.AddSingleton<IAgentDefinition, T>();
    }
    
    /// <summary>
    /// Registers a code-first agent definition instance.
    /// </summary>
    public static IServiceCollection AddAgentDefinition(this IServiceCollection services, IAgentDefinition agentDefinition)
    {
        return services.AddSingleton(agentDefinition);
    }
    
    /// <summary>
    /// Registers a code-first agent workflow definition.
    /// </summary>
    public static IServiceCollection AddAgentWorkflowDefinition<T>(this IServiceCollection services) where T : class, IAgentWorkflowDefinition
    {
        return services.AddSingleton<IAgentWorkflowDefinition, T>();
    }
    
    /// <summary>
    /// Registers a code-first agent workflow definition instance.
    /// </summary>
    public static IServiceCollection AddAgentWorkflowDefinition(this IServiceCollection services, IAgentWorkflowDefinition workflowDefinition)
    {
        return services.AddSingleton(workflowDefinition);
    }
}