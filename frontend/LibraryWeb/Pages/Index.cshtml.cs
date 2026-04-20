using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string SearchQuery { get; set; } = string.Empty;

    public string? Message { get; private set; }

    public string? CurrentUserId => HttpContext.Session.GetString("userId");

    public string? CurrentUserName => HttpContext.Session.GetString("userName");

    public List<BookDto> Books { get; private set; } = [];
    public List<CartFailureDto> Failures { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadFailuresAsync();
    }

    public async Task<IActionResult> OnPostLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            Message = "Enter a user name.";
            return Page();
        }

        var client = _httpClientFactory.CreateClient();
        var usersBase = _configuration["ServiceEndpoints:Users"] ?? "http://localhost:5001";
        var response = await client.GetAsync($"{usersBase}/api/v1/users/by-name/{Uri.EscapeDataString(UserName)}");

        if (!response.IsSuccessStatusCode)
        {
            Message = "User not found. Try user1 or user2.";
            return Page();
        }

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        if (user is null)
        {
            Message = "User lookup failed.";
            return Page();
        }

        HttpContext.Session.SetString("userId", user.Id);
        HttpContext.Session.SetString("userName", user.UserName);

        return RedirectToPage();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDismissFailureAsync(string bookId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage();
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync($"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(bookId)}");

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

        var client = _httpClientFactory.CreateClient();
        var booksBase = _configuration["ServiceEndpoints:Books"] ?? "http://localhost:5002";
        Books = await client.GetFromJsonAsync<List<BookDto>>(
                    $"{booksBase}/api/v1/books/search?query={Uri.EscapeDataString(SearchQuery)}")
                ?? [];

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

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
                await client.PostAsJsonAsync($"{bookingBase}/api/v1/cart/items", 
                    new AddCartItemRequest(userId, bookId, title, author))
            );
            
            if (result.IsSuccessStatusCode)
            {
                Message = "Added to cart.";
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Message = $"Unable to add book to cart - book may be out of stock.";
            }
            else
            {
                Message = "Could not add to cart. Please try again.";
            }
        }
        catch (HttpRequestException ex)
        {
            Message = $"Service temporarily unavailable after retries. ({ex.Message})";
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var booksBase = _configuration["ServiceEndpoints:Books"] ?? "http://localhost:5002";
            Books = await client.GetFromJsonAsync<List<BookDto>>(
                        $"{booksBase}/api/v1/books/search?query={Uri.EscapeDataString(SearchQuery)}")
                    ?? [];
        }

        await LoadFailuresAsync();

        return Page();
    }

    private async Task LoadFailuresAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Failures = [];
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        Failures = await client.GetFromJsonAsync<List<CartFailureDto>>(
                       $"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}")
                   ?? [];
    }

    public sealed record UserDto(string Id, string UserName);

    public sealed record BookDto(string Id, string Title, string Author);

    public sealed record AddCartItemRequest(string UserId, string BookId, string Title, string Author);
}
