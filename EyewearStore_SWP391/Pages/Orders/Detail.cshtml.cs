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

    // ── Helper: parse SnapshotJson for service orders ────────────────────────

    public static (bool isServiceOrder, string? lensName, decimal? lensPrice,
                    string? serviceName, decimal? servicePrice, decimal? framePrice)
        ParseSnapshot(string? snapshotJson)
    {
        if (string.IsNullOrEmpty(snapshotJson))
            return (false, null, null, null, null, null);

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(snapshotJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("isServiceOrder", out var isSvcEl)
                || isSvcEl.ValueKind != System.Text.Json.JsonValueKind.True)
                return (false, null, null, null, null, null);

            string? lensName = root.TryGetProperty("lensProductName", out var ln) ? ln.GetString() : null;
            decimal? lensPrice = root.TryGetProperty("lensPrice", out var lp) && lp.TryGetDecimal(out var lpv) ? lpv : null;
            string? serviceName = root.TryGetProperty("serviceName", out var sn) ? sn.GetString() : null;
            decimal? svcPrice = root.TryGetProperty("servicePrice", out var sp) && sp.TryGetDecimal(out var spv) ? spv : null;
            decimal? framePrice = root.TryGetProperty("framePrice", out var fp) && fp.TryGetDecimal(out var fpv) ? fpv : null;

            return (true, lensName, lensPrice, serviceName, svcPrice, framePrice);
        }
        catch { return (false, null, null, null, null, null); }
    }

    // ── Helper methods ───────────────────────────────────────────────────

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

    public int GetStatusProgress(string status) => status switch
    {
        "Pending" => 10,
        "Pending Confirmation" => 14,
        "Confirmed" => 28,
        "Processing" => 42,
        "Shipped" => 71,
        "Delivered" => 85,
        "Completed" => 100,
        "Cancelled" => 0,
        _ => 0
    };
}
