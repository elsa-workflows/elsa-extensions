using Elsa.Http.Webhooks.Persistence.Entities;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

public class Configurations : IEntityTypeConfiguration<WebhookSinkRecord>
{
    public void Configure(EntityTypeBuilder<WebhookSinkRecord> builder)
    {
        builder.Property(x => x.Headers).HasJsonValueConversion();
        builder.Property(x => x.Filters).HasJsonValueConversion();
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName($"IX_{nameof(WebhookSinkRecord)}_{nameof(WebhookSinkRecord.IsDeleted)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WebhookSinkRecord)}_{nameof(WebhookSinkRecord.Name)}");
    }
}
