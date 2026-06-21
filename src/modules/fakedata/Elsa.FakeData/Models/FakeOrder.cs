namespace Elsa.FakeData.Models;

/// <summary>
/// Represents a fake order record.
/// </summary>
public class FakeOrder
{
    public required Guid Id { get; set; }
    public required string OrderNumber { get; set; }
    public required DateTime OrderDate { get; set; }
    public required string Status { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string ShippingAddress { get; set; }
    public required decimal SubTotal { get; set; }
    public required decimal Tax { get; set; }
    public required decimal Total { get; set; }
}