using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Expressions.JavaScript.TypeDefinitions.Builders;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.WorkflowContexts.Scripting.JavaScript;

/// <summary>
/// Configures the JavaScript engine with functions that allow access to workflow contexts.
/// </summary>
public class ConfigureJavaScriptEngine(ITypeAliasRegistry typeAliasRegistry, ITypeDescriber typeDescriber) : INotificationHandler<EvaluatingJavaScript>, IFunctionDefinitionProvider, ITypeDefinitionProvider
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (!notification.Context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return Task.CompletedTask;

        var providerTypes = GetProviderTypes(workflowExecutionContext);
        var engine = notification.Engine;

        foreach (var providerType in providerTypes)
        {
            var providerName = providerType.GetProviderName();
            var functionName = $"get{providerName}";
            engine.SetValue(functionName, (Func<object?>)(() => workflowExecutionContext.GetWorkflowContext(providerType)));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var providerTypes = GetProviderTypes(context.WorkflowGraph.Workflow);
        var contextTypes = providerTypes.Select(x => x.GetWorkflowContextType());
        var typeDefinitions = contextTypes.Select(x => typeDescriber.DescribeType(x));
        return new(typeDefinitions);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var providerTypes = GetProviderTypes(context.WorkflowGraph.Workflow);
        var functionDefinitions = BuildFunctionDefinitions(providerTypes);
        return new(functionDefinitions);
    }

    private IEnumerable<FunctionDefinition> BuildFunctionDefinitions(IEnumerable<Type> providerTypes)
    {
        foreach (var providerType in providerTypes)
        {
            var builder = new FunctionDefinitionBuilder();
            var providerName = providerType.GetProviderName();
            var functionName = $"get{providerName}";
            var contextType = providerType.GetWorkflowContextType();
            var contextTypeName = typeAliasRegistry.GetAliasOrDefault(contextType, contextType.Name);
            builder.Name(functionName).ReturnType(contextTypeName);

            yield return builder.BuildFunctionDefinition();
        }
    }

    private IEnumerable<Type> GetProviderTypes(WorkflowExecutionContext workflowExecutionContext) => workflowExecutionContext.Workflow.GetWorkflowContextProviderTypes();
    private IEnumerable<Type> GetProviderTypes(Workflow workflow) => workflow.GetWorkflowContextProviderTypes();
}