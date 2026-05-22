
using Elsa.Sql.Client;

namespace Elsa.Sql.Contracts;

public interface ISqlClientResultTypeFactory
{
    ISqlClientResultTypeHandler CreateHandle(string resultType);
}