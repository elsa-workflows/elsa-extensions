using CShells.Features;
using CShells.Hosting;
using Elsa.Scheduling.Quartz.EFCore.SqlServer;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.SqlServer.ShellFeatures;

/// <summary>
/// Configures Quartz to use SQL Server as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz SQL Server Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in SQL Server",
    DependsOn = [typeof(QuartzFeature)])]
[UsedImplicitly]
public class QuartzSqlServerFeature : IShellFeature
{
    /// <summary>The SQL Server connection string.</summary>
    public string ConnectionString { get; set; } = "Server=localhost;Database=Quartz;Trusted_Connection=True;";

    /// <summary>Enable Quartz clustering. Defaults to <c>true</c>.</summary>
    public bool UseClustering { get; set; } = true;

    /// <summary>Use a pooled <c>IDbContextFactory</c>. Defaults to <c>false</c>.</summary>
    public bool UseContextPooling { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (UseContextPooling)
            services.AddPooledDbContextFactory<SqlServerQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<SqlServerQuartzDbContext>(Configure);

        services.AddSingleton<IShellActivatedHandler>(sp =>
            new EfCoreMigrationHandler(sp.GetRequiredService<IDbContextFactory<SqlServerQuartzDbContext>>()));

        // AddQuartz is additive — layers the persistent store onto the base
        // AddQuartz call already made by QuartzFeature (which ran first via DependsOn).
        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseSqlServer(options =>
                {
                    options.ConnectionString = ConnectionString;
                    options.TablePrefix = "[quartz].qrtz_";
                });

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options) =>
        options.UseSqlServer(ConnectionString, sql =>
            sql.MigrationsAssembly(typeof(SqlServerQuartzDbContext).Assembly.GetName().Name));
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
[ShellHandlerOrder(-100)]
file sealed class EfCoreMigrationHandler(IDbContextFactory<SqlServerQuartzDbContext> factory) : IShellActivatedHandler
{
    public async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
