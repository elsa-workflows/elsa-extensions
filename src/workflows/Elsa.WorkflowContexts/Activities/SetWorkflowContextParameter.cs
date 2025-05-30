using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Activities;

/// <summary>
/// Sets a workflow context parameter for a given workflow context provider.
/// </summary>
[Activity("Elsa", "Workflow Context", "Sets a workflow context parameter for a given workflow context provider.")]
[PublicAPI]
public class SetWorkflowContextParameter : CodeActivity
{
    /// <inheritdoc />
    public SetWorkflowContextParameter([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public SetWorkflowContextParameter(Type providerType, string? parameterName, object parameterValue, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        ProviderType = new(providerType);
        ParameterName = new(parameterName);
        ParameterValue = new(parameterValue);
    }
    
    /// <summary>
    /// Create a new instance of <see cref="SetWorkflowContextParameter"/> for the specified provider type.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterValue">The value to set.</param>
    /// <param name="source"></param>
    /// <param name="line"></param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    /// <returns></returns>
    public static SetWorkflowContextParameter For<T>(
        string parameterName, 
        object parameterValue, 
        [CallerFilePath] string? source = null, 
        [CallerLineNumber] int? line = null) where T : IWorkflowContextProvider => new(typeof(T), parameterName, parameterValue, source, line);

    /// <summary>
    /// Create a new instance of <see cref="SetWorkflowContextParameter"/> for the specified provider type.
    /// </summary>
    /// <param name="parameterValue">The value to set.</param>
    /// <param name="source"></param>
    /// <param name="line"></param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    /// <returns></returns>
    public static SetWorkflowContextParameter For<T>(object parameterValue, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) where T : IWorkflowContextProvider
    {
        return new SetWorkflowContextParameter(source, line)
        {
            ProviderType = new(typeof(T)),
            ParameterValue = new(parameterValue)
        };
    }
    
    /// <summary>
    /// Create a new instance of <see cref="SetWorkflowContextParameter"/> for the specified provider type.
    /// </summary>
    /// <param name="parameterValue">The value to set.</param>
    /// <param name="source"></param>
    /// <param name="line"></param>
    /// <typeparam name="T">The type of the workflow context provider.</typeparam>
    /// <returns></returns>
    public static SetWorkflowContextParameter For<T>(Func<ExpressionExecutionContext, object> parameterValue, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) where T : IWorkflowContextProvider
    {
        return new SetWorkflowContextParameter(source, line)
        {
            ProviderType = new(typeof(T)),
            ParameterValue = new(parameterValue)
        };
    }
    
    /// <summary>
    /// The type of the workflow context provider.
    /// </summary>
    [Input(
        UIHint = "workflow-context-provider-picker",
        Description = "The type of the workflow context provider."
    )]
    public Input<Type> ProviderType { get; set; } = null!;
    
    /// <summary>
    /// The name of the parameter to set. If not specified, the parameter name will be inferred from the workflow context provider.
    /// </summary>
    [Input(Description = "Optional. The name of the parameter to set. If not specified, the parameter name will be inferred from the workflow context provider.")]
    public Input<string?> ParameterName { get; set; } = null!;

    /// <summary>
    /// The value of the parameter to set.
    /// </summary>
    [Input(Description = "The value of the parameter to set.")]
    public Input<object> ParameterValue { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var providerType = ProviderType.Get(context);
        var parameterName = ParameterName.GetOrDefault(context);
        var scopedParameterName = providerType.GetScopedParameterName(parameterName);
        var parameterValue = ParameterValue.Get(context);

        // Update the parameter.
        context.WorkflowExecutionContext.SetProperty(scopedParameterName, parameterValue);
        
        // Load the context.
        await context.WorkflowExecutionContext.LoadWorkflowContextAsync(providerType);
    }
}