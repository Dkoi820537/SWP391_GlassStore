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
    /// <summary>
    /// Support Staff Order Details - Process individual orders
    /// Handles prescription verification, customer communication, and order confirmation
    /// </summary>
    [Authorize(Roles = "support,sales,admin")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DetailsModel(EyewearStoreContext context)
        {
            _context = context;
        }

        // Properties
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

        // DTOs
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

        public async Task<IActionResult> OnGetAsync()
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prescription)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Returns)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);

            if (order == null)
            {
                TempData["Error"] = $"Order #{OrderId} not found.";
                return RedirectToPage("./Index");
            }

            Order = order;

            // Customer information
            CustomerName = order.User?.FullName ?? "Unknown";
            CustomerEmail = order.User?.Email ?? "";
            CustomerPhone = order.User?.Phone ?? "";
            CustomerSince = order.User?.CreatedAt ?? DateTime.UtcNow;
            ShippingAddress = order.Address?.AddressLine ?? "No address provided";

            // Map order items
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

            // Determine order characteristics
            HasPrescriptionItems = OrderItems.Any(oi => oi.PrescriptionId != null);
            IsPreOrder = OrderItems.Any(oi => (oi.ProductInventory ?? 0) < oi.Quantity);
            IsHighPriority = HasPrescriptionItems ||
                            order.CreatedAt < DateTime.UtcNow.AddDays(-2) ||
                            OrderItems.Any(oi => oi.HasReturn);

            return Page();
        }

        /// <summary>
        /// BR: Update order status with validation
        /// </summary>
        public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus, string? supportNotes)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("./Index");
            }

            // BR: Validate status transition
            if (!AllStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToPage(new { id = orderId });
            }

            // BR: Special validation for prescription orders
            if (newStatus == "Confirmed" && order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                var unverifiedPrescriptions = order.OrderItems
                    .Where(oi => oi.PrescriptionId != null && !(oi.Prescription?.IsActive ?? false))
                    .ToList();

                if (unverifiedPrescriptions.Any())
                {
                    TempData["Error"] = "Cannot confirm order. Some prescriptions are not verified yet.";
                    return RedirectToPage(new { id = orderId });
                }
            }

            // Update status
            var oldStatus = order.Status;
            order.Status = newStatus;
            _context.Orders.Update(order);

            // BR: Log support notes (implement as needed)
            // await _auditService.LogSupportNotes(orderId, supportNotes, User.Identity.Name);

            await _context.SaveChangesAsync();

            // BR: Trigger notifications based on status change
            await HandleStatusChangeActions(order, oldStatus, newStatus);

            TempData["Success"] = $"Order status updated to: {newStatus}";
            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// BR: Verify prescription data for an order item
        /// </summary>
        public async Task<IActionResult> OnPostVerifyPrescriptionAsync(int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Prescription)
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem == null || orderItem.Prescription == null)
            {
                TempData["Error"] = "Order item or prescription not found.";
                return RedirectToPage(new { id = OrderId });
            }

            // BR: Validate prescription values
            var prescription = orderItem.Prescription;
            var validationErrors = new List<string>();

            // Check SPH range (-20.00 to +20.00)
            if (prescription.LeftSph < -20 || prescription.LeftSph > 20)
                validationErrors.Add("Left eye SPH out of range");
            if (prescription.RightSph < -20 || prescription.RightSph > 20)
                validationErrors.Add("Right eye SPH out of range");

            // Check CYL range (-6.00 to +6.00)
            if (prescription.LeftCyl < -6 || prescription.LeftCyl > 6)
                validationErrors.Add("Left eye CYL out of range");
            if (prescription.RightCyl < -6 || prescription.RightCyl > 6)
                validationErrors.Add("Right eye CYL out of range");

            // Check AXIS range (0 to 180)
            if (prescription.LeftAxis < 0 || prescription.LeftAxis > 180)
                validationErrors.Add("Left eye AXIS out of range");
            if (prescription.RightAxis < 0 || prescription.RightAxis > 180)
                validationErrors.Add("Right eye AXIS out of range");

            if (validationErrors.Any())
            {
                TempData["Error"] = $"Prescription validation failed: {string.Join(", ", validationErrors)}";
                return RedirectToPage(new { id = orderItem.OrderId });
            }

            // Mark as verified
            prescription.IsActive = true;
            _context.PrescriptionProfiles.Update(prescription);
            await _context.SaveChangesAsync();

            // BR: Log verification
            // await _auditService.LogPrescriptionVerified(orderItemId, User.Identity.Name);

            TempData["Success"] = "Prescription verified successfully.";
            return RedirectToPage(new { id = orderItem.OrderId });
        }

        /// <summary>
        /// BR: Quick confirm order (for non-prescription orders)
        /// </summary>
        public async Task<IActionResult> OnPostConfirmAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // BR: Validate prescriptions if any
            if (order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                var unverified = order.OrderItems
                    .Where(oi => oi.PrescriptionId != null && !(oi.Prescription?.IsActive ?? false))
                    .ToList();

                if (unverified.Any())
                {
                    TempData["Error"] = "Please verify all prescriptions before confirming the order.";
                    return RedirectToPage(new { id = orderId });
                }
            }

            order.Status = "Confirmed";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // BR: Send notifications
            // await _notificationService.SendOrderConfirmed(order);
            // await _notificationService.NotifyOperations(order);

            TempData["Success"] = "Order confirmed and sent to Operations team.";
            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// BR: Handle post-status-change actions
        /// </summary>
        private async Task HandleStatusChangeActions(Order order, string oldStatus, string newStatus)
        {
            // BR: Send customer notification when order is confirmed
            if (newStatus == "Confirmed" && oldStatus != "Confirmed")
            {
                // TODO: Implement email service
                // await _emailService.SendOrderConfirmationEmail(order);
            }

            // BR: Notify operations when order is ready for processing
            if (newStatus == "Confirmed")
            {
                // TODO: Implement notification service
                // await _notificationService.NotifyOperations(order);
            }

            // BR: Handle cancellation
            if (newStatus == "Cancelled" && oldStatus != "Cancelled")
            {
                // Restore inventory
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.OrderId)
                    .Include(oi => oi.Product)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (item.Product != null && item.Product.InventoryQty.HasValue)
                    {
                        item.Product.InventoryQty += item.Quantity;
                        _context.Products.Update(item.Product);
                    }
                }
                await _context.SaveChangesAsync();

                // TODO: Send cancellation email
                // await _emailService.SendOrderCancellationEmail(order);
            }
        }
    }
}
