using System.Reflection;
using Elsa.Sql.Contracts;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Sql.UIHints;

/// <summary>
/// Provides registered result types for the ResultType input field.
/// </summary>
/// <param name="sqlClientResultTypesProvider"></param>
public class SqlClientResultTypesDropDownProvider(ISqlClientResultTypesProvider sqlClientResultTypesProvider) : DropDownOptionsProviderBase
{
    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var clients = await sqlClientResultTypesProvider.GetRegisteredSqlResultTypesAsync(cancellationToken);
        return clients.Select(x => new SelectListItem(x.Key, x.Key)).ToList();
    }
}