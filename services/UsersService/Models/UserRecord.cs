namespace UsersService.Models;

// Positional record with a parameterless constructor so EF Core can materialize rows.
public sealed class UserRecord
{
    public UserRecord() { }

    public UserRecord(string id, string userName)
    {
        Id = id;
        UserName = userName;
    }

    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

