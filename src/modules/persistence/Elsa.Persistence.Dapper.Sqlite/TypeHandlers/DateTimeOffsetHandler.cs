namespace Elsa.Persistence.Dapper.Sqlite.TypeHandlers;

/// <summary>
/// Represents a SQLite type handler for <see cref="DateTimeOffset"/>.
/// </summary>
internal class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value) => DateTimeOffset.Parse((string)value);
}