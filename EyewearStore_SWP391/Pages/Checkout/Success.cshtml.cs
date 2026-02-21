using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Success page shown after a successful checkout.
/// Handles both Stripe (session_id) and COD (order_id) flows.
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
    public bool IsCodOrder { get; set; }

    public async Task<IActionResult> OnGetAsync(string? session_id, int? order_id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ── COD path: look up by order_id ──
        if (order_id.HasValue && order_id.Value > 0)
        {
            Order = await _orderService.GetOrderByIdAsync(order_id.Value);

            if (Order == null || Order.UserId != userId)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Cart/Index");
            }

            IsCodOrder = true;

            // Cart should already be cleared in the checkout handler,
            // but clear again idempotently just in case.
            try
            {
                await _cartService.ClearCartAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear cart for user {UserId} on COD success page", userId);
            }

            return Page();
        }

        // ── Stripe path: look up by session_id (existing flow) ──
        if (string.IsNullOrEmpty(session_id))
        {
            TempData["ErrorMessage"] = "Invalid session.";
            return RedirectToPage("/Cart/Index");
        }

        Order = await _orderService.GetOrderByStripeSessionIdAsync(session_id);

        if (Order == null || Order.UserId != userId)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Cart/Index");
        }

        IsCodOrder = false;

        try
        {
            await _cartService.ClearCartAsync(userId);
            _logger.LogInformation(
                "Cart cleared for user {UserId} on checkout success page (Order {OrderId})",
                userId, Order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to clear cart for user {UserId} on success page", userId);
        }

        return Page();
    }
}
