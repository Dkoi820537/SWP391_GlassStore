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
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (Order == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAdvanceStatusAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Shipments)
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

            order.Status = nextStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = nextStatus == "Completed"
                ? $"Order #{orderId} has been completed successfully!"
                : $"Order #{orderId} updated to: {nextStatus}";

            return RedirectToPage(new { id = orderId });
        }

        public async Task<IActionResult> OnPostCreateShipmentAsync(int orderId, string TrackingNumber, string Carrier)
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

                await _context.SaveChangesAsync();

                TempData["Success"] = "Shipment updated successfully.";
            }

            return RedirectToPage(new { id = orderId });
        }

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

            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.InventoryQty = (item.Product.InventoryQty ?? 0) + item.Quantity;
                }
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} has been cancelled and inventory restored.";
            return RedirectToPage(new { id = orderId });
        }
    }
}