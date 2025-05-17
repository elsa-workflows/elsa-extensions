using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Identity.Records;

internal class UserRecord : Record
{
    public string Name { get; set; } = default!;
    public string HashedPassword { get; set; } = default!;
    public string HashedPasswordSalt { get; set; } = default!;
    public string Roles { get; set; } = default!;
}