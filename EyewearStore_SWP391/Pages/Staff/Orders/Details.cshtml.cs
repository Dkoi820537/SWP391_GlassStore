using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Pages.Staff.Orders
{
    [Authorize(Roles = "operational,admin,manager")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DetailsModel(EyewearStoreContext context)
        {
            _context = context;
        }

        private static readonly Dictionary<string, string> OpsNextStatus = new()
        {
            ["Processing"] = "Shipped",
            ["Shipped"] = "Delivered",
            ["Delivered"] = "Completed"
        };

        [BindProperty(SupportsGet = true)]
        public int id { get; set; }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Order = await _context.Orders
                .AsSplitQuery()
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Shipments)
                // ── load history ────────────────────────────────────────────
                .Include(o => o.StatusHistories)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (Order == null)
                return NotFound();

            return Page();
        }

        // ── POST: Advance Status ─────────────────────────────────────────────
        public async Task<IActionResult> OnPostAdvanceStatusAsync(int orderId)
        {
            // Load order items + products only when finalising to Completed,
            // so we avoid the join overhead on every other status advance.
            var order = await _context.Orders
                .Include(o => o.Shipments)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (!OpsNextStatus.TryGetValue(order.Status, out var nextStatus))
            {
                TempData["Error"] = order.Status switch
                {
                    "Completed" => "This order has already been completed.",
                    "Cancelled" => "This order has been cancelled.",
                    "Pending Confirmation" or "Confirmed" => "This order is not yet ready for Operations.",
                    _ => $"Cannot advance from status '{order.Status}'."
                };
                return RedirectToPage(new { id = orderId });
            }

            if (nextStatus == "Shipped")
            {
                var hasShipment = await _context.Shipments
                    .AnyAsync(s => s.OrderId == orderId && !string.IsNullOrEmpty(s.TrackingNumber));

                if (!hasShipment)
                {
                    TempData["Error"] = "Tracking number is required before marking the order as Shipped.";
                    return RedirectToPage(new { id = orderId });
                }
            }

            var prevStatus = order.Status;
            order.Status = nextStatus;

            // ── Soft Allocation → Hard Deduction on Completion ─────────────
            // When the order is physically delivered (Completed), we:
            //   1. Deduct QuantityOnHand  — the physical box has left the warehouse.
            //   2. Deduct AllocatedQuantity — release the reservation that was set
            //      at checkout. Net effect on AvailableStock is zero; it was already
            //      invisible to buyers. We simply clear the accounting entries.
            if (nextStatus == "Completed")
            {
                foreach (var item in order.OrderItems)
                {
                    var product = item.Product;
                    if (product == null) continue;

                    product.QuantityOnHand = Math.Max(0, (product.QuantityOnHand ?? 0) - item.Quantity);
                    product.AllocatedQuantity = Math.Max(0, product.AllocatedQuantity - item.Quantity);
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            // ── Record status history ──────────────────────────────────────
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = nextStatus,
                Actor = "Operations",
                Note = nextStatus == "Shipped"
                    ? "Order packed and handed to carrier"
                    : nextStatus == "Delivered"
                        ? "Delivery confirmed by Operations"
                        : nextStatus == "Completed"
                            ? "Order completed — physical stock deducted"
                            : $"Advanced from {prevStatus} to {nextStatus}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = nextStatus == "Completed"
                ? $"Order #{orderId} has been completed and inventory finalised!"
                : $"Order #{orderId} updated to: {nextStatus}";

            return RedirectToPage(new { id = orderId });
        }

        // ── POST: Create / Update Shipment ────────────────────────────────────
        public async Task<IActionResult> OnPostCreateShipmentAsync(
            int orderId, string TrackingNumber, string Carrier)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (string.IsNullOrWhiteSpace(TrackingNumber))
            {
                TempData["Error"] = "Tracking number is required!";
                return RedirectToPage(new { id = orderId });
            }
            if (string.IsNullOrWhiteSpace(Carrier))
            {
                TempData["Error"] = "Carrier is required!";
                return RedirectToPage(new { id = orderId });
            }

            var existing = await _context.Shipments
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

            if (existing == null)
            {
                var shipment = new Shipment
                {
                    OrderId = order.OrderId,
                    TrackingNumber = TrackingNumber.Trim(),
                    Carrier = Carrier.Trim(),
                    Status = "Shipped",
                    ShippedAt = DateTime.UtcNow
                };

                _context.Shipments.Add(shipment);
                await _context.SaveChangesAsync();

                _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    ShipmentId = shipment.ShipmentId,
                    Status = "Shipped",
                    CreatedAt = DateTime.UtcNow
                });

                // ── Also log in order history ──────────────────────────────
                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = order.Status,
                    Actor = "Operations",
                    Note = $"Tracking assigned: {Carrier} — {TrackingNumber.Trim()}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Shipment created successfully. You can now advance to Shipped.";
            }
            else
            {
                existing.TrackingNumber = TrackingNumber.Trim();
                existing.Carrier = Carrier.Trim();
                existing.ShippedAt = DateTime.UtcNow;

                _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    ShipmentId = existing.ShipmentId,
                    Status = "Updated",
                    CreatedAt = DateTime.UtcNow
                });

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = order.Status,
                    Actor = "Operations",
                    Note = $"Tracking updated: {Carrier} — {TrackingNumber.Trim()}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Shipment updated successfully.";
            }

            return RedirectToPage(new { id = orderId });
        }

        // ── POST: Cancel ─────────────────────────────────────────────────────
        public async Task<IActionResult> OnPostCancelOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (order.Status != "Processing")
            {
                TempData["Error"] = "Operations can only cancel orders in Processing status.";
                return RedirectToPage(new { id = orderId });
            }

            // ── Soft Allocation: release the reservation ───────────────
            // QuantityOnHand is NOT touched — the goods are still in the warehouse.
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.AllocatedQuantity = Math.Max(0, item.Product.AllocatedQuantity - item.Quantity);
                    item.Product.UpdatedAt = DateTime.UtcNow;
                }
            }

            order.Status = "Cancelled";

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = "Cancelled",
                Actor = "Operations",
                Note = "Cancelled by Operations — inventory restored",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} has been cancelled and inventory restored.";
            return RedirectToPage(new { id = orderId });
        }
    }
}