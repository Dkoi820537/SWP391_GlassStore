using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public DetailsModel(EyewearStoreContext context) => _context = context;

        public class ItemDto
        {
            public int OrderItemId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string SnapshotJson { get; set; } = "";
        }

        public class OrderDto
        {
            public int OrderId { get; set; }
            public string UserFullName { get; set; } = "";
            public string UserEmail { get; set; } = "";
            public string UserPhone { get; set; } = "";
            public string AddressLine { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string TrackingNumber { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        [BindProperty]
        public OrderDto Order { get; set; } = new();

        public List<ItemDto> Items { get; set; } = new();

        public List<string> AllStatuses { get; } = new()
        {
            "Pending Confirmation", "Confirmed", "Processing",
            "Shipped", "Delivered", "Completed", "Cancelled"
        };

        // Statuses that are blocked while service job is not Done
        private static readonly string[] LockedStatuses =
            { "Shipped", "Delivered", "Completed" };

        // ── GET ────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var o = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.User)
                .Include(x => x.Address)
                .Include(x => x.Shipments)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (o == null) return NotFound();

            Order = new OrderDto
            {
                OrderId = o.OrderId,
                UserFullName = o.User?.FullName ?? "",
                UserEmail = o.User?.Email ?? "",
                UserPhone = o.User?.Phone ?? "",
                AddressLine = o.Address?.AddressLine ?? "",
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod ?? "",
                TrackingNumber = o.Shipments?.FirstOrDefault()?.TrackingNumber ?? "",
                CreatedAt = o.CreatedAt
            };

            Items = o.OrderItems.Select(oi => new ItemDto
            {
                OrderItemId = oi.OrderItemId,
                ProductName = oi.Product?.Name ?? "(Deleted product)",
                Sku = oi.Product?.Sku ?? "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                SnapshotJson = oi.SnapshotJson ?? ""
            }).ToList();

            return Page();
        }

        // ── POST: Change Status ────────────────────────────────────
        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string newStatus)
        {
            var o = await _context.Orders
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (o == null) return NotFound();

            // Validate
            if (!AllStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToPage(new { id });
            }

            // ── SERVICE LOCK CHECK ─────────────────────────────────
            // If trying to move to a "fulfillment" status, check that
            // every service job in this order is Done (or Cancelled).
            if (LockedStatuses.Contains(newStatus))
            {
                var serviceItems = o.OrderItems
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
                    catch { /* malformed JSON — treat as Pending */ }

                    if (svcStatus != "Done" && svcStatus != "Cancelled")
                    {
                        TempData["Error"] =
                            $"⚠️ Cannot set status to \"{newStatus}\" — " +
                            $"the service job is currently \"{svcStatus}\". " +
                            $"Please wait for the technician team to mark it as Done first.";
                        return RedirectToPage(new { id });
                    }
                }
            }
            // ── END SERVICE LOCK ───────────────────────────────────

            o.Status = newStatus;
            _context.Orders.Update(o);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✓ Order #{id} status updated to: {newStatus}";
            return RedirectToPage(new { id });
        }
    }
}
