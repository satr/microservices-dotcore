using Asp.Versioning;
using BooksService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/books")]
[Authorize] // member or librarian — any authenticated user can browse books
public sealed class BooksController : ControllerBase
{
    private readonly IBookRepository _books;

    public BooksController(IBookRepository books)
    {
        _books = books;
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string query)
    {
        return Ok(_books.Search(query));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var book = _books.GetById(id);
        return book is null ? NotFound() : Ok(book);
    }
}
