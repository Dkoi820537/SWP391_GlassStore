using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Stripe.Checkout;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Success page shown after a successful checkout.
/// All payment methods (Stripe full + COD deposit) now go through Stripe,
/// so the primary path uses session_id. Legacy order_ids path kept as fallback.
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

    /// <summary>Total deposit paid online across all orders.</summary>
    public decimal TotalDepositPaid => Orders.Sum(o => o.DepositAmount);

    /// <summary>Total remaining balance to collect on delivery.</summary>
    public decimal TotalPendingBalance => Orders.Sum(o => o.PendingBalance);

    /// <summary>True if any order in this checkout is COD (deposit-based).</summary>
    public bool IsCodOrder => Orders.Any(o => o.PaymentMethod == "COD");

    /// <summary>True if the checkout produced multiple orders (split-order).</summary>
    public bool IsSplitOrder => Orders.Count > 1;

    public async Task<IActionResult> OnGetAsync(string? session_id, string? order_ids, int? order_id)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ── Legacy COD path (order_ids) — kept as fallback ──────────────────
        if (!string.IsNullOrEmpty(order_ids) || (order_id.HasValue && order_id.Value > 0))
        {
            var ids = new List<int>();

            if (!string.IsNullOrEmpty(order_ids))
            {
                foreach (var s in order_ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(s.Trim(), out var id))
                        ids.Add(id);
                }
            }
            else if (order_id.HasValue)
            {
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

        // ── Primary path: look up by session_id (both Stripe and COD deposit) ──
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

