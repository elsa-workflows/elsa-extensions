namespace Elsa.Persistence.Dapper.Sqlite.TypeHandlers;

/// <summary>
/// Represents a SQLite type handler for <see cref="TimeSpan"/>.
/// </summary>
internal class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value) => TimeSpan.Parse((string)value);
}