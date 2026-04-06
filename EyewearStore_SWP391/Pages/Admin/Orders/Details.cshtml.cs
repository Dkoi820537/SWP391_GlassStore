using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Pages.Admin.Orders
{
    [Authorize(Roles = "admin,manager")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public DetailsModel(EyewearStoreContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public Order? Order { get; set; }

        // Ordered lifecycle steps for the stepper
        public static readonly List<string> LifecycleSteps = new()
        {
            "Pending",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed"
        };

        // Full set of statuses available for override
        public static readonly List<string> AllStatuses = new()
        {
            "Pending", "Pending Confirmation", "Confirmed",
            "Processing", "Processing - Lens Ordered", "Processing - Lens Received",
            "Processing - Fitting", "Processing - QC", "Processing - Packed",
            "Shipped", "Delivered", "Completed", "Cancelled"
        };

        // Map any status to its stepper index (0-based in LifecycleSteps)
        public int ActiveStepIndex
        {
            get
            {
                if (Order == null) return 0;
                string s = Order.Status ?? "";
                if (s == "Pending")              return 0;
                if (s == "Confirmed")            return 1;
                if (s.StartsWith("Processing"))  return 2;
                if (s == "Shipped")              return 3;
                if (s == "Delivered")            return 4;
                if (s == "Completed")            return 5;
                return -1; // Cancelled
            }
        }

        public bool IsCancelled => Order?.Status == "Cancelled";

        // Audit log entries (shipment history + synthetic order events)
        public List<AuditEntry> AuditLog { get; set; } = new();

        public class AuditEntry
        {
            public DateTime Timestamp { get; set; }
            public string   Actor     { get; set; } = "System";
            public string   Event     { get; set; } = "";
            public string   Type      { get; set; } = "info"; // "info" | "warn" | "ok" | "admin"
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Shipments).ThenInclude(s => s.ShipmentStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (Order == null) return NotFound();

            BuildAuditLog();
            return Page();
        }

        // ─── Force to any status ───────────────────────────────────────────────
        public async Task<IActionResult> OnPostForceStatusAsync(int id, string TargetStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            if (!AllStatuses.Contains(TargetStatus))
            {
                TempData["Error"] = $"Invalid target status: {TargetStatus}";
                return RedirectToPage(new { id });
            }

            var prevStatus = order.Status;
            order.Status = TargetStatus;
            _context.Orders.Update(order);

            // If cancelling, restore inventory
            if (TargetStatus == "Cancelled" && prevStatus != "Cancelled")
                await RestoreInventoryAsync(order);

            // Write audit entry into ShipmentStatusHistory
            await WriteAuditEventAsync(order, $"[ADMIN OVERRIDE] {prevStatus} → {TargetStatus}");

            await _context.SaveChangesAsync();
            TempData["Success"] = $"✓ Order #{id} status forced to: {TargetStatus}";
            return RedirectToPage(new { id });
        }


        private async Task WriteAuditEventAsync(Order order, string eventText)
        {
            // Attach to first shipment if exists, else create a minimal one
            var shipment = order.Shipments?.OrderBy(s => s.ShipmentId).FirstOrDefault();
            if (shipment == null)
            {
                var newShipment = new Shipment
                {
                    OrderId   = order.OrderId,
                    Status    = "Audit",
                    ShippedAt = null,
                };
                _context.Shipments.Add(newShipment);
                await _context.SaveChangesAsync(); // flush to get ShipmentId
                shipment = newShipment;
            }

            _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
            {
                ShipmentId = shipment.ShipmentId,
                Status     = eventText,
                CreatedAt  = DateTime.UtcNow
            });
        }

        private async Task RestoreInventoryAsync(Order order)
        {
            var items = await _context.OrderItems
                .Include(i => i.Product)
                .Where(i => i.OrderId == order.OrderId)
                .ToListAsync();
            foreach (var item in items)
            {
                if (item.Product != null)
                {
                    item.Product.InventoryQty = (item.Product.InventoryQty ?? 0) + item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }
        }

        private void BuildAuditLog()
        {
            if (Order == null) return;

            // Order creation
            AuditLog.Add(new AuditEntry
            {
                Timestamp = Order.CreatedAt,
                Actor     = "Customer",
                Event     = $"Order #{Order.OrderId} placed — {Order.TotalAmount:N0} ₫",
                Type      = "ok"
            });

            foreach (var shipment in (Order.Shipments ?? new List<Shipment>()).OrderBy(s => s.ShipmentId))
            {
                if (shipment.ShippedAt.HasValue && shipment.Status != "Audit")
                    AuditLog.Add(new AuditEntry
                    {
                        Timestamp = shipment.ShippedAt.Value,
                        Actor     = "Ops Staff",
                        Event     = $"Shipment created — {shipment.Carrier} / {shipment.TrackingNumber}",
                        Type      = "info"
                    });

                foreach (var h in (shipment.ShipmentStatusHistories ?? new List<ShipmentStatusHistory>())
                                    .OrderBy(h => h.CreatedAt))
                {
                    bool isAdmin = h.Status != null && h.Status.StartsWith("[ADMIN");
                    AuditLog.Add(new AuditEntry
                    {
                        Timestamp = h.CreatedAt,
                        Actor     = isAdmin ? "Admin" : "System",
                        Event     = h.Status ?? "",
                        Type      = isAdmin ? "admin" : "info"
                    });
                }
            }

            // Sort newest first
            AuditLog = AuditLog.OrderByDescending(e => e.Timestamp).ToList();
        }
    }
}
