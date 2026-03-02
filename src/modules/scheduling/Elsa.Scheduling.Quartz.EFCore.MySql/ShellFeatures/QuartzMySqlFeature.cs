using CShells.Features;
using CShells.Hosting;
using Elsa.Scheduling.Quartz.EFCore.MySql;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.MySql.ShellFeatures;

/// <summary>
/// Configures Quartz to use MySQL as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz MySQL Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in MySQL",
    DependsOn = [typeof(QuartzFeature)])]
[UsedImplicitly]
public class QuartzMySqlFeature : IShellFeature
{
    /// <summary>The MySQL connection string.</summary>
    public string ConnectionString { get; set; } = "Server=localhost;Database=quartz;User=root;Password=root;";

    /// <summary>Enable Quartz clustering. Defaults to <c>true</c>.</summary>
    public bool UseClustering { get; set; } = true;

    /// <summary>Use a pooled <c>IDbContextFactory</c>. Defaults to <c>false</c>.</summary>
    public bool UseContextPooling { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (UseContextPooling)
            services.AddPooledDbContextFactory<MySqlQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<MySqlQuartzDbContext>(Configure);

        services.AddSingleton<IShellActivatedHandler>(sp =>
            new EfCoreMigrationHandler(sp.GetRequiredService<IDbContextFactory<MySqlQuartzDbContext>>()));

        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMySqlConnector(options => options.ConnectionString = ConnectionString);

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options) =>
        options.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString), mysql =>
            mysql.MigrationsAssembly(typeof(MySqlQuartzDbContext).Assembly.GetName().Name));
}


/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
[ShellHandlerOrder(-100)]
file sealed class EfCoreMigrationHandler(IDbContextFactory<MySqlQuartzDbContext> factory) : IShellActivatedHandler
{
    public async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
