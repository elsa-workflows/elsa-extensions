using Elsa.Http.Webhooks.Abstractions.Contracts;

namespace Elsa.Http.Webhooks.Api.Contracts;

public static class ModelMapper
{
    public static PersistedWebhookSink ToEntity(this WebhookSinkInputModel model, string? id = null)
    {
        return new PersistedWebhookSink
        {
            Id = id ?? model.Id ?? string.Empty,
            Name = model.Name,
            Description = model.Description,
            Url = model.Url,
            IsEnabled = model.IsEnabled,
            Headers = model.Headers,
            Filters = model.Filters
        };
    }

    public static WebhookSinkModel ToModel(this PersistedWebhookSink entity)
    {
        return new WebhookSinkModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Url = entity.Url,
            IsEnabled = entity.IsEnabled,
            IsDeleted = entity.IsDeleted,
            Headers = entity.Headers,
            Filters = entity.Filters,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
