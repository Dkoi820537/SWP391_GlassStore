using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Orders;

/// <summary>
/// Order history page — lists all orders for the logged-in user.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public List<Order> Orders { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 5;

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (PageIndex < 1) PageIndex = 1;

        var allOrders = await _orderService.GetOrdersByUserIdAsync(userId);
        // Custom orders belong to "My Service Orders" — exclude them here.
        var filteredOrders = allOrders.Where(o => o.OrderType != "Custom").ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var term = SearchQuery.Trim();

            filteredOrders = filteredOrders.Where(o =>
                // Match by Order ID
                o.OrderId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                // Match by product name in order items
                (o.OrderItems != null && o.OrderItems.Any(oi =>
                    oi.Product != null &&
                    oi.Product.Name != null &&
                    oi.Product.Name.Contains(term, StringComparison.OrdinalIgnoreCase))) ||
                // Match by status
                (o.Status != null && o.Status.Contains(term, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        TotalPages = (int)Math.Ceiling(filteredOrders.Count / (double)PageSize);
        if (TotalPages == 0) TotalPages = 1;

        Orders = filteredOrders.Skip((PageIndex - 1) * PageSize).Take(PageSize).ToList();

        return Page();
    }
}
