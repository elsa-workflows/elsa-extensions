using Elsa.Sql.Contracts;
using Elsa.Sql.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Factory;

/// <summary>
/// Provides a factory for creating SQL client result type handlers using dependency injection.
/// </summary>
/// <param name="_serviceProvider">The service provider used to resolve result type handlers.</param>
public class SqlClientResultTypeFactory(IServiceProvider _serviceProvider) : ISqlClientResultTypeFactory
{
    public ISqlClientResultTypeHandler CreateHandle(string resultTypeName)
    {
        if (string.IsNullOrEmpty(resultTypeName))
        {
            throw new ArgumentException($"Result type can not be empty or null.", nameof(resultTypeName));
        }
        if (_serviceProvider.GetRequiredService<ClientStore>().ResultTypes.TryGetValue(resultTypeName, out var resultHandlerType))
        {
            try
            {
                return ActivatorUtilities.CreateInstance(_serviceProvider, resultHandlerType) as ISqlClientResultTypeHandler ?? throw new KeyNotFoundException($"Result handler type '{resultTypeName}' not found.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to create instance of '{resultTypeName}' of type '{resultHandlerType}'.", ex);
            }
        }
        throw new ArgumentException($"No registered SQL result type for '{resultTypeName}'.");
    }

    
}
