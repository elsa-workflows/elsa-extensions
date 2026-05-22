
using Elsa.Sql.Client;

namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypeFactory
{
    /// <summary>
    /// Creates a result type handler for the specified SQL result type.
    /// </summary>
    /// <param name="resultType">The name of the SQL result type.</param>
    /// <returns>An instance of a result type handler for the specified type.</returns>
    ISqlClientResultTypeHandler CreateHandle(string resultType);
}