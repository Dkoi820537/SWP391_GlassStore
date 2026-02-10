using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Orders;

/// <summary>
/// Order detail page — shows full order information including items, address, and status.
/// Only accessible by the order's owner.
/// </summary>
public class DetailModel : PageModel
{
    private readonly IOrderService _orderService;

    public DetailModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Order = await _orderService.GetOrderByIdAsync(id);

        // Security: only the owning user can view
        if (Order == null || Order.UserId != userId)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Orders/Index");
        }

        return Page();
    }
}
