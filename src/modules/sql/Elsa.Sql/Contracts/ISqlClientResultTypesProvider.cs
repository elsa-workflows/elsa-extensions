namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypesProvider
{
    Task<IReadOnlyDictionary<string, Type>> GetRegisteredSqlResultTypesAsync(CancellationToken cancellationToken);
}