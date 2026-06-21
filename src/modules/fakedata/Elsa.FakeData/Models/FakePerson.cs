namespace Elsa.FakeData.Models;

/// <summary>
/// Represents a fake person record.
/// </summary>
public class FakePerson
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required DateTime DateOfBirth { get; set; }
    public required string Gender { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string ZipCode { get; set; }
    public required string Country { get; set; }
}
