namespace Elsa.FakeData.Models;

/// <summary>
/// Represents a fake product record.
/// </summary>
public class FakeProduct
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required string Category { get; set; }
    public required string Department { get; set; }
    public required int Rating { get; set; }
    public required int Stock { get; set; }
}
