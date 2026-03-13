using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Support.Orders
{
    /// <summary>
    /// Full 5-role linear workflow:
    ///
    ///   [Sale/Support]   Pending Confirmation → Confirmed → Processing
    ///   [Operations]     Processing → Shipped → Delivered
    ///   [Manager]        Delivered → Completed
    ///
    /// Each role only sees and can press the ONE button that moves the order
    /// to the NEXT step in their scope. No dropdown, no free-form selection.
    /// </summary>
    [Authorize(Roles = "support,sales,sale,admin,Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DetailsModel(EyewearStoreContext context)
        {
            _context = context;
        }

        // ── Workflow: what Sale/Support is allowed to advance ────────
        private static readonly Dictionary<string, string> SaleNextStatus = new()
        {
            ["Pending Confirmation"] = "Confirmed",
            ["Confirmed"] = "Processing",
        };

        // Statuses that must wait for service job to be Done
        private static readonly HashSet<string> ServiceLockedNextStatuses =
            new() { "Processing" };

        // ── Page properties ──────────────────────────────────────────
        [BindProperty(SupportsGet = true)]
        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;
        public List<OrderItemDto> OrderItems { get; set; } = new();

        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime CustomerSince { get; set; }
        public string ShippingAddress { get; set; } = "";

        public bool HasPrescriptionItems { get; set; }
        public bool IsPreOrder { get; set; }
        public bool IsHighPriority { get; set; }

        public bool HasServiceItem { get; set; }
        public string ServiceStatus { get; set; } = "Pending";
        public string ServiceName { get; set; } = "";
        public string FrameName { get; set; } = "";

        // ── DTOs ─────────────────────────────────────────────────────
        public class OrderItemDto
        {
            public int OrderItemId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public int? PrescriptionId { get; set; }
            public PrescriptionDto? PrescriptionDetails { get; set; }
            public bool IsPrescriptionVerified { get; set; }
            public int? ProductInventory { get; set; }
            public bool HasReturn { get; set; }
            public string SnapshotJson { get; set; } = "";
        }

        public class PrescriptionDto
        {
            public decimal? LeftSph { get; set; }
            public decimal? LeftCyl { get; set; }
            public int? LeftAxis { get; set; }
            public decimal? RightSph { get; set; }
            public decimal? RightCyl { get; set; }
            public int? RightAxis { get; set; }
        }

        // ── GET ───────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync(int id)
        {
            OrderId = id;

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Returns)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);

            if (order == null)
            {
                TempData["Error"] = $"Order #{OrderId} not found.";
                return RedirectToPage("./Index");
            }

            Order = order;
            PopulateCustomerInfo(order);
            PopulateOrderItems(order);
            await ParseServiceInfo();

            return Page();
        }

        // ── POST: Advance Status — the ONLY mutation for Sale/Support ─
        public async Task<IActionResult> OnPostAdvanceStatusAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            // 1. Does this status have a next step for Sale/Support?
            if (!SaleNextStatus.TryGetValue(order.Status, out var nextStatus))
            {
                TempData["Error"] = order.Status switch
                {
                    "Processing" or "Shipped" or "Delivered" or "Completed"
                        => "This order has been handed to Operations / Manager. Sale/Support cannot advance it further.",
                    "Cancelled"
                        => "This order has been cancelled.",
                    _ => $"Cannot advance order from '{order.Status}'."
                };
                return RedirectToPage(new { id = orderId });
            }

            // 2. Prescription must be verified before Confirming
            if (nextStatus == "Confirmed" &&
                order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                var unverified = order.OrderItems
                    .Where(oi => oi.PrescriptionId != null && !(oi.Prescription?.IsActive ?? false))
                    .ToList();

                if (unverified.Any())
                {
                    TempData["Error"] =
                        $"Cannot confirm — {unverified.Count} prescription(s) still need verification.";
                    return RedirectToPage(new { id = orderId });
                }
            }

            // 3. Service-lock: cannot advance to Processing until service job is Done
            if (ServiceLockedNextStatuses.Contains(nextStatus))
            {
                var serviceItems = order.OrderItems
                    .Where(oi => !string.IsNullOrEmpty(oi.SnapshotJson) &&
                                 (oi.SnapshotJson.Contains("\"isServiceOrder\":true") ||
                                  oi.SnapshotJson.Contains("\"lensProductId\":")))
                    .ToList();

                foreach (var item in serviceItems)
                {
                    string svcStatus = "Pending";
                    try
                    {
                        using var doc = JsonDocument.Parse(item.SnapshotJson!);
                        if (doc.RootElement.TryGetProperty("serviceStatus", out var ss))
                            svcStatus = ss.GetString() ?? "Pending";
                    }
                    catch { }

                    if (svcStatus != "Done" && svcStatus != "Cancelled")
                    {
                        TempData["Error"] =
                            $"Cannot advance to \"{nextStatus}\" — service job is still \"{svcStatus}\". " +
                            $"Wait for the technician to mark it Done.";
                        return RedirectToPage(new { id = orderId });
                    }
                }
            }

            // 4. All checks passed — advance
            order.Status = nextStatus;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = nextStatus == "Processing"
                ? $"Order #{orderId} is now Processing — handed off to Operations team!"
                : $"Order #{orderId} advanced to: {nextStatus}";

            return RedirectToPage(new { id = orderId });
        }

        // ── POST: Verify Prescription ─────────────────────────────────
        public async Task<IActionResult> OnPostVerifyPrescriptionAsync(int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Prescription)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem?.Prescription == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToPage(new { id = OrderId });
            }

            var p = orderItem.Prescription;
            var errors = new List<string>();
            if (p.LeftSph < -20 || p.LeftSph > 20) errors.Add("Left SPH out of range (-20 to +20)");
            if (p.RightSph < -20 || p.RightSph > 20) errors.Add("Right SPH out of range (-20 to +20)");
            if (p.LeftCyl < -6 || p.LeftCyl > 6) errors.Add("Left CYL out of range (-6 to +6)");
            if (p.RightCyl < -6 || p.RightCyl > 6) errors.Add("Right CYL out of range (-6 to +6)");
            if (p.LeftAxis < 0 || p.LeftAxis > 180) errors.Add("Left AXIS out of range (0-180)");
            if (p.RightAxis < 0 || p.RightAxis > 180) errors.Add("Right AXIS out of range (0-180)");

            if (errors.Any())
            {
                TempData["Error"] = $"Prescription validation failed: {string.Join(", ", errors)}";
                return RedirectToPage(new { id = orderItem.OrderId });
            }

            p.IsActive = true;
            _context.PrescriptionProfiles.Update(p);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Prescription verified!";
            return RedirectToPage(new { id = orderItem.OrderId });
        }

        // ── POST: Escalate to Manager ─────────────────────────────────
        public async Task<IActionResult> OnPostEscalateAsync(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            TempData["Success"] = $"Order #{orderId} has been flagged for Manager review.";
            return RedirectToPage(new { id = orderId });
        }

        // ── POST: Cancel Order ────────────────────────────────────────
        public async Task<IActionResult> OnPostCancelOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (order.Status != "Pending Confirmation")
            {
                TempData["Error"] = "Only orders in 'Pending Confirmation' can be cancelled by Support.";
                return RedirectToPage(new { id = orderId });
            }

            order.Status = "Cancelled";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} has been cancelled.";
            return RedirectToPage(new { id = orderId });
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void PopulateCustomerInfo(Order order)
        {
            CustomerName = order.User?.FullName ?? "Unknown";
            CustomerEmail = order.User?.Email ?? "";
            CustomerPhone = order.User?.Phone ?? "";
            CustomerSince = order.User?.CreatedAt ?? DateTime.UtcNow;
            ShippingAddress = order.Address?.AddressLine ?? order.AddressLine ?? "No address provided";
        }

        private void PopulateOrderItems(Order order)
        {
            OrderItems = order.OrderItems.Select(oi => new OrderItemDto
            {
                OrderItemId = oi.OrderItemId,
                ProductName = oi.Product?.Name ?? "(Deleted Product)",
                Sku = oi.Product?.Sku ?? "N/A",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                PrescriptionId = oi.PrescriptionId,
                PrescriptionDetails = oi.Prescription != null ? new PrescriptionDto
                {
                    LeftSph = oi.Prescription.LeftSph,
                    LeftCyl = oi.Prescription.LeftCyl,
                    LeftAxis = oi.Prescription.LeftAxis,
                    RightSph = oi.Prescription.RightSph,
                    RightCyl = oi.Prescription.RightCyl,
                    RightAxis = oi.Prescription.RightAxis
                } : null,
                IsPrescriptionVerified = oi.Prescription?.IsActive ?? false,
                ProductInventory = oi.Product?.InventoryQty,
                HasReturn = oi.Returns.Any(),
                SnapshotJson = oi.SnapshotJson ?? ""
            }).ToList();

            HasPrescriptionItems = OrderItems.Any(oi => oi.PrescriptionId != null);
            IsPreOrder = OrderItems.Any(oi => (oi.ProductInventory ?? 0) < oi.Quantity);
            IsHighPriority = HasPrescriptionItems
                                   || Order.CreatedAt < DateTime.UtcNow.AddDays(-2)
                                   || OrderItems.Any(oi => oi.HasReturn);
        }

        private Task ParseServiceInfo()
        {
            var svcItem = OrderItems.FirstOrDefault(oi =>
                !string.IsNullOrEmpty(oi.SnapshotJson) &&
                (oi.SnapshotJson.Contains("\"isServiceOrder\":true") ||
                 oi.SnapshotJson.Contains("\"lensProductId\":")));

            if (svcItem == null) return Task.CompletedTask;

            HasServiceItem = true;
            try
            {
                using var doc = JsonDocument.Parse(svcItem.SnapshotJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("serviceStatus", out var ss)) ServiceStatus = ss.GetString() ?? "Pending";
                if (root.TryGetProperty("serviceName", out var sn)) ServiceName = sn.GetString() ?? "";
                if (root.TryGetProperty("frameName", out var fn)) FrameName = fn.GetString() ?? "";
                else if (root.TryGetProperty("productName", out var pn)) FrameName = pn.GetString() ?? "";
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
