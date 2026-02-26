using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Persistence.MongoDb.Entities;

public class WebhookSinkDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = default!;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public List<WebhookEventFilter> Filters { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Version { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
