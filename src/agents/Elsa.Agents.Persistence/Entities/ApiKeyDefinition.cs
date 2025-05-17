using Elsa.Agents;
using Elsa.Common.Entities;
using JetBrains.Annotations;

namespace Elsa.Agents.Persistence.Entities;

[UsedImplicitly]
public class ApiKeyDefinition : Entity
{
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;

    public ApiKeyConfig ToApiKeyConfig()
    {
        return new ApiKeyConfig
        {
            Name = Name,
            Value = Value
        };
    }
    
    public ApiKeyModel ToModel()
    {
        return new ApiKeyModel
        {
            Id = Id,
            Name = Name,
            Value = Value
        };
    }
}