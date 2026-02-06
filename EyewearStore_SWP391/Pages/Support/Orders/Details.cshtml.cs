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
    [Authorize(Roles = "support,sales,admin")]
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

        // DTOs (same as before)
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

        // ---------- Existing handlers (UpdateStatus, VerifyPrescription, Confirm) remain same ----------
        // I assume you already have OnPostUpdateStatusAsync, OnPostVerifyPrescriptionAsync, OnPostConfirmAsync implemented.
        // Keep them as in your current code (no changes required).

        /// <summary>
        /// Escalate to Manager (simple implementation: create TempData message and (TODO) send notification)
        /// </summary>
        public async Task<IActionResult> OnPostEscalateAsync(int orderId, string? escalateNote)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("./Index");
            }

            // For now: do not change DB. Place a TODO to send real notification/email to manager.
            // Example: _emailService.SendEscalationEmail(managerEmail, order, escalateNote);
            // We'll just set TempData and return.

            TempData["Success"] = $"Order #{orderId} escalated to manager. Note: {escalateNote ?? "(no note)"}";

            // Optionally: log to audit table (if implemented). For now skip DB changes.

            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// Cancel order (simple: set status = Cancelled and optionally save reason)
        /// </summary>
        public async Task<IActionResult> OnPostCancelAsync(int orderId, string? cancelReason)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToPage("./Index");
            }

            if (order.Status == "Cancelled")
            {
                TempData["Error"] = "Order already cancelled.";
                return RedirectToPage(new { id = orderId });
            }

            order.Status = "Cancelled";
            // Optionally append cancel reason to internal notes if the model has one (your Order doesn't have InternalNotes currently).
            _context.Orders.Update(order);

            // restore inventory for items (same logic as other parts)
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null && item.Product.InventoryQty.HasValue)
                {
                    item.Product.InventoryQty += item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} cancelled. Reason: {cancelReason ?? "(none)"}";
            return RedirectToPage(new { id = orderId });
        }

        /// <summary>
        /// Request prescription correction (placeholder) - currently will create a notification (TODO).
        /// </summary>
        public async Task<IActionResult> OnPostRequestCorrectionAsync(int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Prescription)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem == null)
            {
                TempData["Error"] = "Order item not found.";
                return RedirectToPage(new { id = OrderId });
            }

            // TODO: Send email to customer asking to correct prescription
            TempData["Success"] = "Requested prescription correction from customer (placeholder).";

            return RedirectToPage(new { id = orderItem.OrderId });
        }
    }
}