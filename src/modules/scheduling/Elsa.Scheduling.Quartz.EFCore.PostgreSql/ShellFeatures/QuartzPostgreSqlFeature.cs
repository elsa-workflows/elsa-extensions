using CShells.Features;
using CShells.Hosting;
using Elsa.Scheduling.Quartz.EFCore.PostgreSql;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.PostgreSql.ShellFeatures;

/// <summary>
/// Configures Quartz to use PostgreSQL as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz PostgreSQL Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in PostgreSQL",
    DependsOn = [typeof(QuartzFeature)])]
[UsedImplicitly]
public class QuartzPostgreSqlFeature : IShellFeature
{
    /// <summary>The PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = "Host=localhost;Database=quartz;Username=postgres;Password=postgres";

    /// <summary>Enable Quartz clustering. Defaults to <c>true</c>.</summary>
    public bool UseClustering { get; set; } = true;

    /// <summary>Use a pooled <c>IDbContextFactory</c>. Defaults to <c>false</c>.</summary>
    public bool UseContextPooling { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (UseContextPooling)
            services.AddPooledDbContextFactory<PostgreSqlQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<PostgreSqlQuartzDbContext>(Configure);

        services.AddSingleton<IShellActivatedHandler>(sp =>
            new EfCoreMigrationHandler(sp.GetRequiredService<IDbContextFactory<PostgreSqlQuartzDbContext>>()));

        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UsePostgres(options =>
                {
                    options.ConnectionString = ConnectionString;
                    options.TablePrefix = "quartz.qrtz_";
                });

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options) =>
        options.UseNpgsql(ConnectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(PostgreSqlQuartzDbContext).Assembly.GetName().Name));
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
[ShellHandlerOrder(-100)]
file sealed class EfCoreMigrationHandler(IDbContextFactory<PostgreSqlQuartzDbContext> factory) : IShellActivatedHandler
{
    public async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
