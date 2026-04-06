using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Stripe.Checkout;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Success page shown after a successful checkout.
/// Handles both Stripe (session_id) and COD (order_ids) flows.
/// Supports split-order checkout: displays multiple order confirmations.
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

    /// <summary>All orders from this checkout (may be 1 or 2 for split-order).</summary>
    public List<Order> Orders { get; set; } = new();

    /// <summary>Grand total across all orders in this checkout group.</summary>
    public decimal GrandTotal => Orders.Sum(o => o.TotalAmount);

    public bool IsCodOrder { get; set; }

    /// <summary>True if the checkout produced multiple orders (split-order).</summary>
    public bool IsSplitOrder => Orders.Count > 1;

    public async Task<IActionResult> OnGetAsync(string? session_id, string? order_ids, int? order_id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ── COD path: look up by order_ids (comma-separated) or legacy order_id ──
        if (!string.IsNullOrEmpty(order_ids) || (order_id.HasValue && order_id.Value > 0))
        {
            var ids = new List<int>();

            if (!string.IsNullOrEmpty(order_ids))
            {
                // New split-order format: "123,456"
                foreach (var s in order_ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(s.Trim(), out var id))
                        ids.Add(id);
                }
            }
            else if (order_id.HasValue)
            {
                // Legacy single-order format
                ids.Add(order_id.Value);
            }

            foreach (var id in ids)
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order != null && order.UserId == userId)
                    Orders.Add(order);
            }

            if (!Orders.Any())
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Cart/Index");
            }

            IsCodOrder = true;

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

        // ── Stripe path: look up by session_id ──
        if (string.IsNullOrEmpty(session_id))
        {
            TempData["ErrorMessage"] = "Invalid session.";
            return RedirectToPage("/Cart/Index");
        }

        // Find ALL orders sharing this Stripe session (split-order support)
        Orders = await _orderService.GetOrdersByStripeSessionIdAsync(session_id);

        // Filter to current user
        Orders = Orders.Where(o => o.UserId == userId).ToList();

        if (!Orders.Any())
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToPage("/Cart/Index");
        }

        IsCodOrder = false;

        // Fallback for local testing without webhooks: Check status and manually mark as paid
        var pendingOrders = Orders.Where(o => o.Status == "Pending").ToList();
        if (pendingOrders.Any())
        {
            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(session_id);

                if (session.PaymentStatus == "paid" && !string.IsNullOrEmpty(session.PaymentIntentId))
                {
                    foreach (var order in pendingOrders)
                    {
                        await _orderService.MarkOrderPaidAsync(order.OrderId, session.PaymentIntentId);
                    }
                    // Reload all orders
                    Orders = await _orderService.GetOrdersByStripeSessionIdAsync(session_id);
                    Orders = Orders.Where(o => o.UserId == userId).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify Stripe session on success page");
            }
        }

        try
        {
            await _cartService.ClearCartAsync(userId);
            _logger.LogInformation(
                "Cart cleared for user {UserId} on checkout success page ({OrderCount} orders)",
                userId, Orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to clear cart for user {UserId} on success page", userId);
        }

        return Page();
    }
}
