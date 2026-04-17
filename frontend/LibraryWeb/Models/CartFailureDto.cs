namespace LibraryWeb.Models;

public sealed record CartFailureDto(
    string BookId,
    string Title,
    string Author,
    string Reason,
    DateTime FailedAtUtc);

