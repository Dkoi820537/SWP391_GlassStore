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

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Orders = await _orderService.GetOrdersByUserIdAsync(userId);
        return Page();
    }

    public string GetStatusBadgeClass(string status) => status switch
    {
        "Pending" => "bg-warning text-dark",
        "Confirmed" => "bg-info text-dark",
        "Processing" => "bg-primary text-white",
        "Shipped" => "bg-secondary text-white",
        "Delivered" => "bg-success text-white",
        "Completed" => "bg-success text-white",
        "Cancelled" => "bg-danger text-white",
        "Pending Confirmation" => "bg-warning text-dark",
        _ => "bg-secondary text-white"
    };

    public string GetStatusIcon(string status) => status switch
    {
        "Pending" => "bi-hourglass-split",
        "Pending Confirmation" => "bi-hourglass-split",
        "Confirmed" => "bi-check-circle",
        "Processing" => "bi-gear",
        "Shipped" => "bi-truck",
        "Delivered" => "bi-box-seam",
        "Completed" => "bi-check-all",
        "Cancelled" => "bi-x-circle",
        _ => "bi-question-circle"
    };
}
