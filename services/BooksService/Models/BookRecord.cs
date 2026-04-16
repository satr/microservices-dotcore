namespace BooksService.Models;

// Class instead of record so EF Core can materialize rows without a parameterless constructor requirement.
public sealed class BookRecord
{
    public BookRecord() { }

    public BookRecord(string id, string title, string author)
    {
        Id = id;
        Title = title;
        Author = author;
    }

    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
}

