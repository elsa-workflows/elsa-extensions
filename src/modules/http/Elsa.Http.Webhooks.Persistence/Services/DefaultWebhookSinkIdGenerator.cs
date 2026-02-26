using Elsa.Http.Webhooks.Abstractions.Contracts;

namespace Elsa.Http.Webhooks.Persistence.Services;

public class DefaultWebhookSinkIdGenerator : IGenerateWebhookSinkId
{
    public string Generate() => Guid.NewGuid().ToString("N");
}
