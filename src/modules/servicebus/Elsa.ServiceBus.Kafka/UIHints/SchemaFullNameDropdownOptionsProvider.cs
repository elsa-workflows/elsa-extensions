using System.Reflection;
using Confluent.SchemaRegistry;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.ServiceBus.Kafka.UIHints;

/// <summary>
/// Populates the SchemaFullName dropdown with Avro record full names fetched from all configured
/// schema registries. Always includes a leading "(any)" option so the user can leave filtering disabled.
/// Fails gracefully when no schema registries are configured or a registry is unreachable.
/// </summary>
public class SchemaFullNameDropdownOptionsProvider(ISchemaRegistryDefinitionEnumerator registryEnumerator) : DropDownOptionsProviderBase
{
    private static readonly SelectListItem AnyOption = new("(any)", "");

    protected override async ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var items = new List<SelectListItem> { AnyOption };
        var registries = await registryEnumerator.EnumerateAsync(cancellationToken);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var registry in registries)
        {
            // SaslInherit bridges credentials from a ConsumerConfig we don't have here; skip silently.
            if (registry.Config.BasicAuthCredentialsSource == AuthCredentialsSource.SaslInherit)
                continue;

            try
            {
                using var client = new CachedSchemaRegistryClient(registry.Config);
                var subjects = await client.GetAllSubjectsAsync();

                foreach (var subject in subjects)
                {
                    // Key subjects are typically primitives (string/long), not interesting for filtering.
                    if (subject.EndsWith("-key", StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        var registered = await client.GetLatestSchemaAsync(subject);

                        if (registered.SchemaType != SchemaType.Avro)
                            continue;

                        var schema = Avro.Schema.Parse(registered.SchemaString);

                        if (schema is not Avro.RecordSchema recordSchema)
                            continue;

                        var fullName = recordSchema.Fullname;

                        if (!string.IsNullOrWhiteSpace(fullName) && seen.Add(fullName))
                            items.Add(new SelectListItem(fullName, fullName));
                    }
                    catch
                    {
                        // Subject schema unreachable or unparseable; skip it.
                    }
                }
            }
            catch
            {
                // Registry unreachable or misconfigured; skip it and keep the "(any)" option.
            }
        }

        return items;
    }
}
