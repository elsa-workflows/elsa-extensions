using Elsa.Http.Webhooks.Persistence.Entities;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyWebhookPersistence(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSinkRecord>(builder => builder.ConfigureWebhookSinkRecord());
        return modelBuilder;
    }

    public static EntityTypeBuilder<WebhookSinkRecord> ConfigureWebhookSinkRecord(this EntityTypeBuilder<WebhookSinkRecord> builder)
    {
        builder.Property(x => x.Headers).HasJsonValueConversion();
        builder.Property(x => x.Filters).HasJsonValueConversion();
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName($"IX_{nameof(WebhookSinkRecord)}_{nameof(WebhookSinkRecord.IsDeleted)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WebhookSinkRecord)}_{nameof(WebhookSinkRecord.Name)}");
        return builder;
    }
}
