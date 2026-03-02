using CShells.Features;
using CShells.Hosting;
using Elsa.Scheduling.Quartz.EFCore.Sqlite;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.Sqlite.ShellFeatures;

/// <summary>
/// Configures Quartz to use SQLite as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz SQLite Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in SQLite",
    DependsOn = [typeof(QuartzFeature)])]
[UsedImplicitly]
public class QuartzSqliteFeature : IShellFeature, IPostConfigureShellServices
{
    /// <summary>The SQLite connection string.</summary>
    public string ConnectionString { get; set; } = "Data Source=quartz.db";

    /// <summary>
    /// Enable Quartz clustering. Defaults to <c>false</c> — SQLite does not
    /// support true multi-node clustering.
    /// </summary>
    public bool UseClustering { get; set; }

    /// <summary>Use a pooled <c>IDbContextFactory</c>. Defaults to <c>false</c>.</summary>
    public bool UseContextPooling { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (UseContextPooling)
            services.AddPooledDbContextFactory<SqliteQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<SqliteQuartzDbContext>(Configure);

        services.AddSingleton<IShellActivatedHandler>(sp =>
            new EfCoreMigrationHandler(
                sp.GetRequiredService<IDbContextFactory<SqliteQuartzDbContext>>()));
    }

    public void PostConfigureServices(IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMicrosoftSQLite(ConnectionString);

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options) =>
        options.UseSqlite(ConnectionString, sqlite =>
            sqlite.MigrationsAssembly(typeof(SqliteQuartzDbContext).Assembly.GetName().Name));
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
[ShellHandlerOrder(-100)]
file sealed class EfCoreMigrationHandler(IDbContextFactory<SqliteQuartzDbContext> factory) : IShellActivatedHandler
{
    public async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
