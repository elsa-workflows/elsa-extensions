using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Extensions;
using Elsa.Testing.Core.Abstractions;
using Elsa.Testing.Core.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Testing.Core.Serialization;

public class AssertionSerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public AssertionSerializer(IOptions<TestingOptions> options)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type != typeof(Assertion))
                    return;

                if (typeInfo.Kind != JsonTypeInfoKind.Object)
                    return;
            
                var polymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "Type"
                };

                foreach (var type in options.Value.AssertionTypes.ToList())
                    polymorphismOptions.DerivedTypes.Add(new(type, type.Name));

                typeInfo.PolymorphismOptions = polymorphismOptions;
            }),
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        }.WithConverters(new JsonStringEnumConverter());
        
        _serializerOptions = serializerOptions;
    }

    public JsonSerializerOptions SerializerOptions => _serializerOptions;
    public string Serialize(Assertion assertion) => JsonSerializer.Serialize(assertion, _serializerOptions);
    public string Serialize(IEnumerable<Assertion> assertions) => JsonSerializer.Serialize(assertions, _serializerOptions);
    public Assertion Deserialize(string json) => JsonSerializer.Deserialize<Assertion>(json, _serializerOptions)!;
    public IEnumerable<Assertion> DeserializeMany(string json) => JsonSerializer.Deserialize<IEnumerable<Assertion>>(json, _serializerOptions)!;
}