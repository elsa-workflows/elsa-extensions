using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentMigrator;
using JetBrains.Annotations;
using Elsa.Persistence.Dapper.Migrations;

namespace Elsa.Persistence.Dapper.Migrations.Identity;

/// <summary>
/// Adds a unique index on <c>Roles (TenantId, Name)</c> so two tenants can share a role name
/// while a single tenant cannot (#282 / elsa-core#8615).
/// </summary>
/// <remarks>
/// 3.9 databases have no name uniqueness, so this migration never rewrites or deletes rows.
/// Same-tenant duplicate names (including case-insensitive pairs, which RoleManager treats
/// as the same role) fail the migration with the colliding ids. The operator must resolve
/// those rows and re-run. <c>NULL</c> and <c>''</c> tenant ids are treated as the default
/// tenant for that check, matching Dapper's read filter. The index itself is on the stored
/// columns. SQL Server treats NULLs as equal in a unique index (one <c>(NULL, Name)</c>
/// per name). SQLite, PostgreSQL, MySQL and Oracle treat NULLs as distinct, so a later
/// <c>NULL</c>/<c>''</c> pair is not rejected by the index (elsa-extensions#245 / #242
/// normalisation). The index follows the database collation: typically case-insensitive
/// on SQL Server, case-sensitive on SQLite / PostgreSQL / MySQL / Oracle. Case variants
/// on SQLite and PostgreSQL therefore rely on core's <c>OrdinalIgnoreCase</c> pre-save
/// check; the index only rejects exact stored names there.
/// </remarks>
[Migration(30005, "Elsa:Identity:V3.10")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_10 : Migration
{
    /// <summary>
    /// Unique index on <c>Roles (TenantId, Name)</c>.
    /// </summary>
    public const string TenantIdNameUniqueIndex = "IX_Roles_TenantId_Name";

    /// <inheritdoc />
    public override void Up()
    {
        if (!Schema.Table("Roles").Exists() || !Schema.Table("Roles").Column("TenantId").Exists())
            return;

        if (Schema.Table("Roles").Index(TenantIdNameUniqueIndex).Exists())
            return;

        IfDatabase(MigrationDatabases.QuotedIdentifiers)
            .Execute.WithConnection((connection, transaction) =>
                ThrowIfDuplicateTenantRoleNames(connection, transaction, quoted: true));
        IfDatabase(MigrationDatabases.UnquotedIdentifiers)
            .Execute.WithConnection((connection, transaction) =>
                ThrowIfDuplicateTenantRoleNames(connection, transaction, quoted: false));

        Create.Index(TenantIdNameUniqueIndex)
            .OnTable("Roles")
            .OnColumn("TenantId").Ascending()
            .OnColumn("Name").Ascending()
            .WithOptions().Unique();
    }

    /// <inheritdoc />
    public override void Down()
    {
        if (Schema.Table("Roles").Index(TenantIdNameUniqueIndex).Exists())
            Delete.Index(TenantIdNameUniqueIndex).OnTable("Roles");
    }

    internal static void ThrowIfDuplicateTenantRoleNames(IDbConnection connection, IDbTransaction? transaction, bool quoted = false)
    {
        var roles = ReadRoles(connection, transaction, quoted);
        var duplicates = roles
            .GroupBy(role => (TenantKey: NormalizeTenantKey(role.TenantId), NameKey: role.Name.ToLowerInvariant()))
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key.TenantKey)
            .ThenBy(group => group.Key.NameKey)
            .ToList();

        if (duplicates.Count == 0)
            return;

        var message = new StringBuilder();
        message.Append("Cannot create unique index ")
            .Append(TenantIdNameUniqueIndex)
            .Append(" on Roles (TenantId, Name): the table already has same-tenant duplicate names. ")
            .Append("No rows were changed. Rename or delete the extra rows and re-run the migration.");

        foreach (var group in duplicates)
        {
            message.AppendLine()
                .Append("  tenant ")
                .Append(FormatTenant(group.Key.TenantKey))
                .Append(", name(s) ")
                .Append(string.Join(" / ", group.Select(role => $"'{role.Name}'").Distinct(StringComparer.Ordinal)))
                .Append(": ")
                .Append(string.Join(", ", group.Select(role => $"Id={role.Id}")));
        }

        throw new InvalidOperationException(message.ToString());
    }

    private static List<RoleNameRow> ReadRoles(IDbConnection connection, IDbTransaction? transaction, bool quoted)
    {
        var rows = new List<RoleNameRow>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = quoted
            ? "SELECT \"Id\", \"TenantId\", \"Name\" FROM \"Roles\""
            : "SELECT Id, TenantId, Name FROM Roles";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new RoleNameRow(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetString(2)));
        }

        return rows;
    }

    private static string NormalizeTenantKey(string? tenantId) =>
        string.IsNullOrEmpty(tenantId) ? string.Empty : tenantId;

    private static string FormatTenant(string tenantKey) =>
        tenantKey.Length == 0 ? "(default)" : $"'{tenantKey}'";

    private sealed record RoleNameRow(string Id, string? TenantId, string Name);
}
