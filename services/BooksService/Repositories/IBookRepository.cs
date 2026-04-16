using BooksService.Models;

namespace BooksService.Repositories;

public interface IBookRepository
{
    IReadOnlyList<BookRecord> Search(string query);
    BookRecord? GetById(string id);
}

