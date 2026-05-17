namespace Elsa.FakeData.Models;

/// <summary>
/// Represents a fake user account record.
/// </summary>
public class FakeUser
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Avatar { get; set; }
    public required string[] Roles { get; set; }
    public required DateTime CreatedAt { get; set; }
}
