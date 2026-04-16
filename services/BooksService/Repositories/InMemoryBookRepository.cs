using BooksService.Models;

namespace BooksService.Repositories;

public sealed class InMemoryBookRepository : IBookRepository
{
    private static readonly BookRecord[] Books =
    [
        new("b1", "Book1", "Author1"),
        new("b2", "Book2", "Author2"),
        new("b3", "Book3", "Author3")
    ];

    public IReadOnlyList<BookRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return Books
            .Where(book => book.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public BookRecord? GetById(string id)
    {
        return Books.FirstOrDefault(book => string.Equals(book.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}

