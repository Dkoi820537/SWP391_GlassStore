using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EyewearStore_SWP391.Pages.Support.Orders
{
    [Authorize(Roles = "support,sales,admin,Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DetailsModel(EyewearStoreContext context)
        {
            _context = context;
        }

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

        public List<string> AllStatuses { get; } = new()
        {
            "Pending Confirmation",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed",
            "Cancelled"
        };

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
            CustomerName = order.User?.FullName ?? "Unknown";
            CustomerEmail = order.User?.Email ?? "";
            CustomerPhone = order.User?.Phone ?? "";
            CustomerSince = order.User?.CreatedAt ?? DateTime.UtcNow;
            ShippingAddress = order.Address?.AddressLine ?? "No address provided";

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
                HasReturn = oi.Returns.Any()
            }).ToList();

            HasPrescriptionItems = OrderItems.Any(oi => oi.PrescriptionId != null);
            IsPreOrder = OrderItems.Any(oi => (oi.ProductInventory ?? 0) < oi.Quantity);
            IsHighPriority = HasPrescriptionItems ||
                            order.CreatedAt < DateTime.UtcNow.AddDays(-2) ||
                            OrderItems.Any(oi => oi.HasReturn);

            return Page();
        }

        /// <summary>
        /// Update order status
        /// </summary>
        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("./Index");
            }

            if (!AllStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToPage(new { id = orderId });
            }

            // Validate prescription verification for Confirmed status
            if (newStatus == "Confirmed" && order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                var unverified = order.OrderItems
                    .Where(oi => oi.PrescriptionId != null && !(oi.Prescription?.IsActive ?? false))
                    .ToList();

                if (unverified.Any())
                {
                    TempData["Error"] = "Cannot confirm order. Some prescriptions are not verified yet.";
                    return RedirectToPage(new { id = orderId });
                }
            }

            var oldStatus = order.Status;
            order.Status = newStatus;
            _context.Orders.Update(order);

            // Handle inventory restoration for cancelled orders
            if (newStatus == "Cancelled" && oldStatus != "Cancelled")
            {
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Product)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (item.Product?.InventoryQty != null)
                    {
                        item.Product.InventoryQty += item.Quantity;
                        _context.Products.Update(item.Product);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // TODO: Send notification email
            // await SendStatusUpdateEmail(order, oldStatus, newStatus);

            TempData["Success"] = $"Order status updated to: {newStatus}";
            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// Verify prescription values
        /// </summary>
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

            // BR-S003: Validate prescription ranges
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

            // Mark as verified
            p.IsActive = true;
            _context.PrescriptionProfiles.Update(p);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✓ Prescription verified successfully!";
            return RedirectToPage(new { id = orderItem.OrderId });
        }

        /// <summary>
        /// Quick confirm order
        /// </summary>
        public async Task<IActionResult> OnPostConfirmAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            // Check if all prescriptions are verified
            if (order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                var unverified = order.OrderItems
                    .Where(oi => oi.PrescriptionId != null && !(oi.Prescription?.IsActive ?? false))
                    .ToList();

                if (unverified.Any())
                {
                    TempData["Error"] = "⚠️ Please verify all prescriptions before confirming!";
                    return RedirectToPage(new { id = orderId });
                }
            }

            order.Status = "Confirmed";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // TODO: Send confirmation email
            // await _emailService.SendOrderConfirmedEmail(order);

            TempData["Success"] = "✓ Order confirmed and sent to Operations team!";
            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// Escalate to Manager - Creates a new Order entry or uses separate Escalation table
        /// </summary>
        public async Task<IActionResult> OnPostEscalateAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("./Index");
            }

            // TODO: Create escalation record in a separate table
            // Or send notification to manager
            // await _notificationService.EscalateToManager(order, User.Identity.Name);

            // For now, just show success message
            TempData["Success"] = $"⬆️ Order #{orderId} escalated to Manager for review!";
            return RedirectToPage(new { id = orderId });
        }
    }
}
