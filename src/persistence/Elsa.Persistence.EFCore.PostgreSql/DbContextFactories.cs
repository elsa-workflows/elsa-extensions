using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Elsa.Persistence.EFCore.PostgreSql;

[UsedImplicitly]
public class IdentityDbContextFactory : PostgreSqlDesignTimeDbContextFactory<IdentityElsaDbContext>;

[UsedImplicitly]
public class ManagementDbContextFactory : PostgreSqlDesignTimeDbContextFactory<ManagementElsaDbContext>;

[UsedImplicitly]
public class RuntimeDbContextFactory : PostgreSqlDesignTimeDbContextFactory<RuntimeElsaDbContext>;

[UsedImplicitly]
public class LabelsDbContextFactory : PostgreSqlDesignTimeDbContextFactory<LabelsElsaDbContext>;

[UsedImplicitly]
public class AlterationsDbContextFactories : PostgreSqlDesignTimeDbContextFactory<AlterationsElsaDbContext>;

[UsedImplicitly]
public class TenantsDbContextFactories : PostgreSqlDesignTimeDbContextFactory<TenantsElsaDbContext>;

public class PostgreSqlDesignTimeDbContextFactory<TDbContext> : DesignTimeDbContextFactoryBase<TDbContext> where TDbContext : DbContext
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<TDbContext> builder, string connectionString)
    {
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
    }
}