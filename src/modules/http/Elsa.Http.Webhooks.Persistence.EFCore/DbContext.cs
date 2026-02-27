using Elsa.Http.Webhooks.Persistence.Entities;
using Elsa.Persistence.EFCore;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

[UsedImplicitly]
public class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options, IServiceProvider serviceProvider) : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<WebhookSinkRecord> WebhookSinks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations());
        base.OnModelCreating(modelBuilder);
    }
}
