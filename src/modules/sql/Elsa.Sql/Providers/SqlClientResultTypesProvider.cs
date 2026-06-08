using Elsa.Sql.Contracts;
using Elsa.Sql.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Providers;

/// <summary>
/// Returns registered result types for SQL clients
/// </summary>
public class SqlClientResultTypesProvider : ISqlClientResultTypesProvider
{
    private readonly IServiceProvider _serviceProvider;

    public SqlClientResultTypesProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task<IReadOnlyDictionary<string, Type>> GetRegisteredSqlResultTypesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_serviceProvider.GetRequiredService<ClientStore>().ResultTypes);
    }
}