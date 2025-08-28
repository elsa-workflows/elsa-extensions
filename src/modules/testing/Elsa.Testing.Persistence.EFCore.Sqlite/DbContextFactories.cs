using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Testing.Persistence.EFCore.Sqlite;

[UsedImplicitly]
public class SqliteTestingDbContextFactory : DesignTimeDbContextFactoryBase<TestingDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TestingDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlite(GetType().Assembly, connectionString);
    }
}