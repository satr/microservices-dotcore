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
    public string? CurrentUserName => HttpContext.Session.GetString("userName");

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
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync($"{bookingBase}/api/cart/items/{Uri.EscapeDataString(bookId)}?userId={Uri.EscapeDataString(userId)}");

        await LoadCartAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDismissFailureAsync(string bookId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.DeleteAsync($"{bookingBase}/api/cart/failures/{Uri.EscapeDataString(userId)}/{Uri.EscapeDataString(bookId)}");

        await LoadCartAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage("/Index");
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        await client.PostAsync($"{bookingBase}/api/cart/complete?userId={Uri.EscapeDataString(userId)}", null);

        Message = "Borrowing completed!";
        await Task.Delay(800); // brief pause for saga processing
        await LoadCartAsync();
        return Page();
    }

    private async Task LoadCartAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            Items = [];
            Failures = [];
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var bookingBase = _configuration["ServiceEndpoints:Booking"] ?? "http://localhost:5003";
        Items = await client.GetFromJsonAsync<List<CartItemDto>>(
                    $"{bookingBase}/api/cart/{Uri.EscapeDataString(userId)}")
                ?? [];

        Failures = await client.GetFromJsonAsync<List<CartFailureDto>>(
                       $"{bookingBase}/api/cart/failures/{Uri.EscapeDataString(userId)}")
                   ?? [];
    }

    public sealed record CartItemDto(string BookId, string Title, string Author);
}

