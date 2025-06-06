using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Dapper.Models;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ParameterizedQuery"/> to build SQL queries.
/// </summary>
[PublicAPI]
public static class ParameterizedQueryBuilderExtensions
{
    /// <summary>
    /// Begins a SELECT FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    public static ParameterizedQuery From(this ParameterizedQuery query, string table)
    {
        query.Sql.AppendLine(query.Dialect.From(table));
        return query;
    }

    /// <summary>
    /// Begins a SELECT FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <param name="fields">The fields.</param>
    public static ParameterizedQuery From(this ParameterizedQuery query, string table, params string[] fields)
    {
        query.Sql.AppendLine(query.Dialect.From(table, fields));
        return query;
    }

    /// <summary>
    /// Begins a SELECT FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <typeparam name="T">The fields to include based on the public properties of the specified type.</typeparam>
    public static ParameterizedQuery From<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this ParameterizedQuery query, string table) => query.From(table, typeof(T));

    /// <summary>
    /// Begins a SELECT FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <param name="modelType">The fields to include based on the public properties of the specified type.</param>
    public static ParameterizedQuery From(this ParameterizedQuery query, string table, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type modelType)
    {
        // Create a list of fields based on the public properties of the specified type.
        var fields = modelType.GetProperties().Select(x => x.Name).ToArray();
        query.Sql.AppendLine(query.Dialect.From(table, fields));
        return query;
    }

    /// <summary>
    /// Begins a DELETE FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    public static ParameterizedQuery Delete(this ParameterizedQuery query, string table)
    {
        query.Sql.AppendLine(query.Dialect.Delete(table));
        return query;
    }

    /// <summary>
    /// Begins a DELETE FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <param name="primaryKey">The primary key.</param>
    /// <param name="innerQuery">The inner query.</param>
    public static ParameterizedQuery Delete(this ParameterizedQuery query, string table, string primaryKey, ParameterizedQuery innerQuery)
    {
        query.Sql.AppendLine(query.Dialect.Delete(table));
        query.Sql.AppendLine($"and {primaryKey} in (");
        query.Sql.AppendLine(innerQuery.Sql.ToString());
        query.Sql.AppendLine(")");
        return query;
    }

    /// <summary>
    /// Begins a SELECT COUNT(*) FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    public static ParameterizedQuery Count(this ParameterizedQuery query, string table)
    {
        query.Sql.AppendLine(query.Dialect.Count(table));
        return query;
    }

    /// <summary>
    /// Begins a SELECT COUNT(expression) FROM query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="expression">The expression. Examples: "*" or "DISTINCT FieldName"</param>
    /// <param name="table">The table.</param>
    public static ParameterizedQuery Count(this ParameterizedQuery query, string expression, string table)
    {
        query.Sql.AppendLine(query.Dialect.Count(expression, table));
        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="value">The value.</param>
    public static ParameterizedQuery Is(this ParameterizedQuery query, string field, object? value)
    {
        if (value == null) return query;
        if (value is DBNull) return IsNull(query, field);
        query.Sql.AppendLine(query.Dialect.And(field));
        query.Parameters.Add($"@{field}", value);

        return query;
    }

    /// <summary>
    /// Appends a negating AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="value">The value.</param>
    public static ParameterizedQuery IsNot(this ParameterizedQuery query, string field, object? value)
    {
        if (value == null) return query;
        if (value is DBNull) return IsNotNull(query, field);

        query.Sql.AppendLine(query.Dialect.AndNot(field));
        query.Parameters.Add($"@{field}", value);

        return query;
    }

    /// <summary>
    /// Appends an IS NULL clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    public static ParameterizedQuery IsNull(this ParameterizedQuery query, string field)
    {
        query.Sql.AppendLine(query.Dialect.IsNull(field));
        return query;
    }

    /// <summary>
    /// Appends an IS NOT NULL clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    public static ParameterizedQuery IsNotNull(this ParameterizedQuery query, string field)
    {
        query.Sql.AppendLine(query.Dialect.IsNotNull(field));
        return query;
    }

    /// <summary>
    /// Appends a search term for workflow definitions to the search.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="searchTerm">The search term.</param>
    public static ParameterizedQuery WorkflowDefinitionSearchTerm(this ParameterizedQuery query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return query;

        var searchTermLike = $"%{searchTerm}%";
        query.Sql.AppendLine("and (Name like @SearchTermLike or Description like @SearchTermLike or Id like @SearchTerm or DefinitionId like @SearchTerm)");
        query.Parameters.Add("@SearchTerm", searchTerm);
        query.Parameters.Add("@SearchTermLike", searchTermLike);
        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="values">The values.</param>
    public static ParameterizedQuery In(this ParameterizedQuery query, string field, IEnumerable<object>? values)
    {
        var valueList = values?.ToList();

        if (valueList == null || !valueList.Any()) return query;

        var fieldParamNames = valueList
            .Select((_, index) => $"@{field}{index}")
            .ToArray();

        query.Sql.AppendLine(query.Dialect.And(field, fieldParamNames));

        for (var i = 0; i < fieldParamNames.Length; i++)
            query.Parameters.Add(fieldParamNames[i], valueList.ElementAt(i));

        return query;
    }
    
    /// <summary>
    /// Appends a negating AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="values">The values.</param>
    public static ParameterizedQuery NotIn(this ParameterizedQuery query, string field, IEnumerable<object>? values)
    {
        var valueList = values?.ToList();

        if (valueList == null || !valueList.Any()) return query;

        var fieldParamNames = valueList
            .Select((_, index) => $"@{field}{index}")
            .ToArray();

        query.Sql.AppendLine(query.Dialect.AndNot(field, fieldParamNames));

        for (var i = 0; i < fieldParamNames.Length; i++)
            query.Parameters.Add(fieldParamNames[i], valueList.ElementAt(i));

        return query;
    }

    public static ParameterizedQuery StartsWith(this ParameterizedQuery query, string field, bool startsWith, string? value)
    {
        if (!startsWith || value == null || string.IsNullOrWhiteSpace(value))
            return query;

        var searchTermLike = $"{value}%";
        query.Sql.AppendLine($"and {field} like @SearchTermLike");
        query.Parameters.Add($"@{field}", searchTermLike);

        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the value is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="versionOptions">The version options.</param>
    public static ParameterizedQuery Is(this ParameterizedQuery query, VersionOptions? versionOptions)
    {
        if (versionOptions == null) return query;

        var sql = query.Sql;
        var options = versionOptions.Value;
        if (options.IsDraft) sql.AppendLine("and IsPublished = 0");
        if (options.IsLatest) sql.AppendLine("and IsLatest = 1");
        if (options.IsPublished) sql.AppendLine("and IsPublished = 1");
        if (options.IsLatestOrPublished) sql.AppendLine("and (IsLatest = 1 or IsPublished = 1)");
        if (options.IsLatestAndPublished) sql.AppendLine("and IsLatest = 1 and IsPublished = 1");
        if (options.Version > 0)
        {
            sql.AppendLine(query.Dialect.And("Version"));
            query.Parameters.Add("@Version", options.Version);
        }

        return query;
    }

    /// <summary>
    /// Appends an AND clause to the query if the search term is not null.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="searchTerm">The search term.</param>
    public static ParameterizedQuery AndWorkflowInstanceSearchTerm(this ParameterizedQuery query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return query;

        var searchTermLike = $"%{searchTerm}%";
        query.Sql.AppendLine("and (Name like @SearchTermLike or ID like @SearchTerm or DefinitionId like @SearchTerm or DefinitionVersionId like @SearchTerm or CorrelationId like @SearchTerm)");
        query.Parameters.Add("@SearchTerm", searchTerm);
        query.Parameters.Add("@SearchTermLike", searchTermLike);
        return query;
    }

    /// <summary>
    /// Appends an ORDER BY clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="keySelector">A lambda expression that selects the field to order by.</param>
    /// <param name="direction">The order direction.</param>
    /// <typeparam name="TRecord">The type of the record.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public static ParameterizedQuery OrderBy<TRecord, TField>(this ParameterizedQuery query, Expression<Func<TRecord, TField>> keySelector, OrderDirection direction)
    {
        var fieldName = keySelector.GetPropertyName();
        return query.OrderBy(fieldName, direction);
    }

    /// <summary>
    /// Appends an ORDER BY clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="field">The field.</param>
    /// <param name="direction">The direction.</param>
    public static ParameterizedQuery OrderBy(this ParameterizedQuery query, string field, OrderDirection direction)
    {
        var directionString = direction == OrderDirection.Ascending ? "asc" : "desc";
        query.Sql.AppendLine($"order by {field} {directionString}");
        return query;
    }

    /// <summary>
    /// Appends an ORDER BY clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="orderFields">The fields by which to order.</param>
    public static ParameterizedQuery OrderBy(this ParameterizedQuery query, params OrderField[] orderFields)
    {
        if (!orderFields.Any())
            return query;

        var clauses = string.Join(",", orderFields.Select(x => $"{x.Field} {(x.Direction == OrderDirection.Ascending ? "asc" : "desc")}"));
        query.Sql.AppendLine($"order by {clauses}");
        return query;
    }

    /// <summary>
    /// Appends an OFFSET clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="skip">The number of records to skip.</param>
    public static ParameterizedQuery Skip(this ParameterizedQuery query, int skip)
    {
        query.Sql.AppendLine(query.Dialect.Skip(skip));
        return query;
    }

    /// <summary>
    /// Appends a LIMIT clause to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="take">The number of records to take.</param>
    public static ParameterizedQuery Take(this ParameterizedQuery query, int take)
    {
        query.Sql.AppendLine(query.Dialect.Take(take));
        return query;
    }

    /// <summary>
    /// Applies paging to the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="pageArgs">The page arguments.</param>
    public static ParameterizedQuery Page(this ParameterizedQuery query, PageArgs pageArgs)
    {
        query.Sql.AppendLine(query.Dialect.Page(pageArgs));
        return query;
    }

    /// <summary>
    /// Appends a statement that updates or inserts a record.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <param name="primaryKeyField">The primary key field.</param>
    /// <param name="record">The record.</param>
    /// <param name="getParameterName">An optional function to get the parameter name.</param>
    public static ParameterizedQuery Upsert(this ParameterizedQuery query, string table, string primaryKeyField, object record, Func<string, string>? getParameterName = null)
    {
        var fields = record.GetType().GetProperties()
            .Where(x => x.CanRead && x.Name != primaryKeyField)
            .Select(x => x.Name)
            .ToArray();

        getParameterName ??= x => x;

        query.Sql.AppendLine(query.Dialect.Upsert(table, primaryKeyField, fields, getParameterName));

        var primaryKeyValue = record.GetType().GetProperty(primaryKeyField)?.GetValue(record);
        query.Parameters.Add($"@{getParameterName(primaryKeyField)}", primaryKeyValue);

        var recordType = record.GetType();
        foreach (var field in fields)
        {
            var prop = recordType.GetProperty(field)!;
            var propType = prop.PropertyType;
            var value = prop.GetValue(record);
            var dbType = value == null ? GetDbType(propType) : null;
            query.Parameters.Add($"@{getParameterName(field)}", value, dbType);
        }

        return query;
    }

    /// <summary>
    /// Appends a statement that inserts a record.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="table">The table.</param>
    /// <param name="record">The record.</param>
    /// <param name="getParameterName">An optional function to get the parameter name.</param>
    public static ParameterizedQuery Insert(this ParameterizedQuery query, string table, object record, Func<string, string>? getParameterName = null)
    {
        var fields = record.GetType().GetProperties()
            .Select(x => x.Name)
            .ToArray();

        getParameterName ??= x => x;
        query.Sql.AppendLine(query.Dialect.Insert(table, fields, getParameterName));

        foreach (var field in fields)
        {
            var value = record.GetType().GetProperty(field)?.GetValue(record);
            query.Parameters.Add($"@{getParameterName(field)}", value);
        }

        return query;
    }
    
    /// <summary>
    /// Appends a statement that updates a record.
    /// </summary>
    public static ParameterizedQuery Update(this ParameterizedQuery query, string table, object record, string primaryKeyField, Func<string, string>? getParameterName = null)
    {
        var fields = record.GetType().GetProperties()
            .Where(x => x.CanRead && x.Name != primaryKeyField)
            .Select(x => x.Name)
            .ToArray();

        return Update(query, table, record, primaryKeyField, fields, getParameterName);
    }

    /// <summary>
    /// Constructs an UPDATE query for the specified table and applies the provided record, primaryKeyField, and specified fields.
    /// </summary>
    /// <param name="query">The query being built.</param>
    /// <param name="table">The name of the table to update.</param>
    /// <param name="record">The object containing the values to be updated.</param>
    /// <param name="primaryKeyField">The name of the primary key field to identify the record.</param>
    /// <param name="fields">An array of field names to include in the update statement.</param>
    /// <param name="getParameterName">An optional function to customize parameter names for the query.</param>
    /// <returns>Returns the updated instance of <see cref="ParameterizedQuery"/>.</returns>
    public static ParameterizedQuery Update(this ParameterizedQuery query, string table, object record, string primaryKeyField, string[] fields, Func<string, string>? getParameterName = null)
    {
        getParameterName ??= x => x;
        query.Sql.AppendLine(query.Dialect.Update(table, primaryKeyField, fields, getParameterName));

        var primaryKeyValue = record.GetType().GetProperty(primaryKeyField)?.GetValue(record);
        query.Parameters.Add($"@{getParameterName(primaryKeyField)}", primaryKeyValue);

        var recordType = record.GetType();
        foreach (var field in fields)
        {
            var prop = recordType.GetProperty(field)!;
            var propType = prop.PropertyType;
            var value = prop.GetValue(record);
            var dbType = value == null ? GetDbType(propType) : null;
            query.Parameters.Add($"@{getParameterName(field)}", value, dbType);
        }

        return query;
    }

    private static DbType? GetDbType(Type type)
    {
        if (type == typeof(byte[])) return DbType.Binary;
        return null;
    }
}