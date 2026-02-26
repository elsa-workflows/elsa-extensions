using Elsa.Http.Webhooks.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

public class Configurations : IEntityTypeConfiguration<WebhookSinkRecord>
{
    public void Configure(EntityTypeBuilder<WebhookSinkRecord> builder)
    {
        builder.ConfigureWebhookSinkRecord();
    }
}
