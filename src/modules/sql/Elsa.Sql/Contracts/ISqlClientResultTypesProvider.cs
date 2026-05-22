namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypesProvider
{
    /// <summary>
    /// Gets a dictionary of registered SQL result types and their corresponding .NET types.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A dictionary mapping SQL result type names to their corresponding .NET types.</returns>
    Task<IReadOnlyDictionary<string, Type>> GetRegisteredSqlResultTypesAsync(CancellationToken cancellationToken);
}