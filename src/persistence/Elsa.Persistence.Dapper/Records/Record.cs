namespace Elsa.Persistence.Dapper.Records;

public class Record
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
}