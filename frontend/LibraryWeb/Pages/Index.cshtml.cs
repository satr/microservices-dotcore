using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using LibraryWeb.Models;
using Polly;

namespace LibraryWeb.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, _) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                });
    }

    [BindProperty]
    public string SearchQuery { get; set; } = string.Empty;

    public string? Message { get; private set; }

    // Identity comes from OIDC cookie — no manual session for authentication
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public string? CurrentUserName => User.FindFirst("preferred_username")?.Value
                                      ?? User.Identity?.Name;
    public string? CurrentUserId => HttpContext.Session.GetString("userId");
    public bool IsLibrarian => User.IsInRole("librarian");

    public List<BookDto> Books { get; private set; } = [];
    public List<CartFailureDto> Failures { get; private set; } = [];

    public async Task OnGetAsync()
    {
        if (IsAuthenticated)
        {
            await EnsureUserIdInSessionAsync();
            await LoadFailuresAsync();
        }
    }

    /// After OIDC login the userId (from users-service) is resolved once and cached in session.
    private async Task EnsureUserIdInSessionAsync()
    {
        if (HttpContext.Session.GetString("userId") != null) return;

        var userName = CurrentUserName;
        if (string.IsNullOrWhiteSpace(userName)) return;

        var client = await CreateAuthenticatedClientAsync();
        var usersBase = _configuration["ServiceEndpoints:Users"] ?? "http://localhost:5001";
        var response = await client.GetAsync(
            $"{usersBase}/api/v1/users/by-name/{Uri.EscapeDataString(userName)}");

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user != null)
            {
                HttpContext.Session.SetString("userId", user.Id);
            }
        }
    }

    public IActionResult OnPostLogout()
    {
        // Logout is handled by POST /logout endpoint in Program.cs (OIDC sign-out)
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostDismissFailureAsync(string bookId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return RedirectToPage();

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync(
            $"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(bookId)}");

        await LoadFailuresAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            Message = "Enter a book title to search.";
            return Page();
        }

        var client = await CreateAuthenticatedClientAsync();
        var booksBase = _configuration["ServiceEndpoints:Books"] ?? "http://localhost:5002";
        var raw = await client.GetFromJsonAsync<List<BookDtoRaw>>(
                      $"{booksBase}/api/v1/books/search?query={Uri.EscapeDataString(SearchQuery)}")
                  ?? [];

        Books = await EnrichWithStockAsync(client, raw);
        await LoadFailuresAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(string bookId, string title, string author)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Message = "Log in before adding items.";
            return Page();
        }

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
                await client.PostAsJsonAsync($"{bookingBase}/api/v1/cart/items",
                    new AddCartItemRequest(userId, bookId, title, author))
            );

            Message = result.IsSuccessStatusCode
                ? "Added to cart."
                : result.StatusCode == System.Net.HttpStatusCode.BadRequest
                    ? "Unable to add book to cart — book may be out of stock."
                    : "Could not add to cart. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            Message = $"Service temporarily unavailable after retries. ({ex.Message})";
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var booksBase = _configuration["ServiceEndpoints:Books"] ?? "http://localhost:5002";
            var raw = await client.GetFromJsonAsync<List<BookDtoRaw>>(
                          $"{booksBase}/api/v1/books/search?query={Uri.EscapeDataString(SearchQuery)}")
                      ?? [];
            Books = await EnrichWithStockAsync(client, raw);
        }

        await LoadFailuresAsync();
        return Page();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// Creates an HttpClient with the OIDC access token in the Authorization header.
    /// This propagates the JWT to downstream services for both authentication and audit.
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<List<BookDto>> EnrichWithStockAsync(HttpClient client, List<BookDtoRaw> books)
    {
        if (books.Count == 0) return [];
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        var ids = books.Select(b => b.Id).ToList();
        Dictionary<string, int>? stockMap = null;
        try
        {
            var resp = await client.PostAsJsonAsync($"{bookingBase}/api/v1/inventory/stock/batch", ids);
            stockMap = await resp.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        }
        catch { /* stock unavailable */ }

        return books.Select(b => new BookDto(b.Id, b.Title, b.Author,
            stockMap?.GetValueOrDefault(b.Id, -1) ?? -1)).ToList();
    }

    private async Task LoadFailuresAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) { Failures = []; return; }

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        Failures = await client.GetFromJsonAsync<List<CartFailureDto>>(
                       $"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}")
                   ?? [];
    }

    public sealed record UserDto(string Id, string UserName);
    public sealed record BookDtoRaw(string Id, string Title, string Author);
    public sealed record BookDto(string Id, string Title, string Author, int Stock);

    public sealed record AddCartItemRequest(string UserId, string BookId, string Title, string Author);
}
