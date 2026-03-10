using System.Reflection;
using Elsa.Mqtt.Options;
using Elsa.Workflows.UIHints.Dropdown;
using Microsoft.Extensions.Options;

namespace Elsa.Mqtt.UIHints;

internal class MqttConnectionDropdownOptionsProvider(IOptions<MqttOptions> ldapOptions) : DropDownOptionsProviderBase
{
    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var dropdownOptions = ldapOptions.Value.Connections.Keys
            .Select(connectionName => new SelectListItem(connectionName, connectionName))
            .ToList();

        return ValueTask.FromResult<ICollection<SelectListItem>>(dropdownOptions);
    }
}