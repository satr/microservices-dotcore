using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LibraryWeb.Models;

namespace LibraryWeb.Pages;

public class CartModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CartModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public string? CurrentUserId => HttpContext.Session.GetString("userId");
    public string? CurrentUserName => User.FindFirst("preferred_username")?.Value ?? User.Identity?.Name;

    public string? Message { get; private set; }

    public List<CartItemDto> Items { get; private set; } = [];
    public List<CartFailureDto> Failures { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadCartAsync();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string bookId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return RedirectToPage("/Index");

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync(
            $"{bookingBase}/api/v1/cart/items/{Uri.EscapeDataString(bookId)}?userId={Uri.EscapeDataString(userId)}");

        await LoadCartAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDismissFailureAsync(string bookId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return RedirectToPage("/Index");

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync(
            $"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(bookId)}");

        await LoadCartAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return RedirectToPage("/Index");

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.PostAsync(
            $"{bookingBase}/api/v1/cart/complete?userId={Uri.EscapeDataString(userId)}", null);

        Message = "Borrowing completed!";
        await Task.Delay(800);
        await LoadCartAsync();
        return Page();
    }

    private async Task LoadCartAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) { Items = []; Failures = []; return; }

        var client = await CreateAuthenticatedClientAsync();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        Items = await client.GetFromJsonAsync<List<CartItemDto>>(
                    $"{bookingBase}/api/v1/cart/{Uri.EscapeDataString(userId)}")
                ?? [];
        Failures = await client.GetFromJsonAsync<List<CartFailureDto>>(
                       $"{bookingBase}/api/v1/cart/failures/{Uri.EscapeDataString(userId)}")
                   ?? [];
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var token = await HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public sealed record CartItemDto(string BookId, string Title, string Author);
}
