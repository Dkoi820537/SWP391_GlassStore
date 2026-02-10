using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Success page shown after a successful Stripe Checkout.
/// Receives session_id from Stripe redirect URL, looks up the order, and displays confirmation.
/// Also clears the user's cart so they see an empty cart immediately after checkout.
/// </summary>
public class SuccessModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(
        IOrderService orderService,
        ICartService cartService,
        ILogger<SuccessModel> logger)
    {
        _orderService = orderService;
        _cartService = cartService;
        _logger = logger;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(string? session_id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        if (string.IsNullOrEmpty(session_id))
        {
            TempData["ErrorMessage"] = "Invalid session.";
            return RedirectToPage("/Cart/Index");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Order = await _orderService.GetOrderByStripeSessionIdAsync(session_id);

        // Security: only show if the order belongs to this user
        if (Order == null || Order.UserId != userId)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Cart/Index");
        }

        // Clear the cart immediately so the user sees it empty.
        // This is safe even if the webhook already cleared it (ClearCartAsync is idempotent).
        try
        {
            await _cartService.ClearCartAsync(userId);
            _logger.LogInformation(
                "Cart cleared for user {UserId} on checkout success page (Order {OrderId})",
                userId, Order.OrderId);
        }
        catch (Exception ex)
        {
            // Don't block the success page if cart clearing fails
            _logger.LogWarning(ex,
                "Failed to clear cart for user {UserId} on success page", userId);
        }

        return Page();
    }
}

