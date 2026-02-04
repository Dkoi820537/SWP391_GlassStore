using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class EditModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public EditModel(EyewearStoreContext context) => _context = context;

        public class OrderEditDto
        {
            public int OrderId { get; set; }

            [Required(ErrorMessage = "Status is required")]
            public string Status { get; set; } = "";

            [StringLength(100, ErrorMessage = "Tracking number cannot exceed 100 characters")]
            public string TrackingNumber { get; set; } = "";

            [Required(ErrorMessage = "Payment method is required")]
            public string PaymentMethod { get; set; } = "";
        }

        [BindProperty]
        public OrderEditDto Order { get; set; } = new();

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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Shipments)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = $"Order #{id} not found";
                return RedirectToPage("Index");
            }

            // Get tracking number from first shipment if exists
            var trackingNumber = order.Shipments?.FirstOrDefault()?.TrackingNumber ?? "";

            Order = new OrderEditDto
            {
                OrderId = order.OrderId,
                Status = order.Status,
                TrackingNumber = trackingNumber,
                PaymentMethod = order.PaymentMethod ?? "COD"
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var order = await _context.Orders
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync(o => o.OrderId == Order.OrderId);

            if (order == null)
            {
                TempData["Error"] = $"Order #{Order.OrderId} not found";
                return RedirectToPage("Index");
            }

            // Validate status
            if (!AllStatuses.Contains(Order.Status))
            {
                ModelState.AddModelError("Order.Status", "Invalid status selected");
                return Page();
            }

            // Validate payment method
            var validPaymentMethods = new[] { "COD", "Online" };
            if (!validPaymentMethods.Contains(Order.PaymentMethod))
            {
                ModelState.AddModelError("Order.PaymentMethod", "Invalid payment method selected");
                return Page();
            }

            // BR: Validate status transition
            if (!ValidateStatusTransition(order.Status, Order.Status))
            {
                ModelState.AddModelError("Order.Status",
                    $"Cannot change status from '{order.Status}' to '{Order.Status}'");
                return Page();
            }

            // Update order
            var oldStatus = order.Status;
            order.Status = Order.Status;
            order.PaymentMethod = Order.PaymentMethod;
            _context.Orders.Update(order);

            // Handle shipment tracking
            var shipment = order.Shipments?.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(Order.TrackingNumber))
            {
                if (shipment == null)
                {
                    // Create new shipment
                    shipment = new Shipment
                    {
                        OrderId = order.OrderId,
                        TrackingNumber = Order.TrackingNumber,
                        Carrier = "Default Carrier",
                        Status = "Label Created",
                        ShippedAt = Order.Status == "Shipped" ? DateTime.UtcNow : (DateTime?)null
                    };
                    _context.Shipments.Add(shipment);
                }
                else
                {
                    // Update existing shipment
                    shipment.TrackingNumber = Order.TrackingNumber;

                    // Update shipment status based on order status
                    if (Order.Status == "Shipped" && shipment.ShippedAt == null)
                    {
                        shipment.ShippedAt = DateTime.UtcNow;
                        shipment.Status = "In Transit";
                    }
                    else if (Order.Status == "Delivered" && shipment.DeliveredAt == null)
                    {
                        shipment.DeliveredAt = DateTime.UtcNow;
                        shipment.Status = "Delivered";
                    }

                    _context.Shipments.Update(shipment);
                }
            }
            else if (shipment != null)
            {
                // Clear tracking if empty
                shipment.TrackingNumber = "";
                _context.Shipments.Update(shipment);
            }

            await _context.SaveChangesAsync();

            // BR: Handle post-update actions
            await HandleStatusChangeActions(order, oldStatus, Order.Status);

            TempData["Success"] = $"Order #{order.OrderId} updated successfully";
            return RedirectToPage("Details", new { id = order.OrderId });
        }

        /// <summary>
        /// BR: Validate if status transition is allowed
        /// </summary>
        private bool ValidateStatusTransition(string currentStatus, string newStatus)
        {
            // No change needed
            if (currentStatus == newStatus) return true;

            // Allow any transition to Cancelled
            if (newStatus == "Cancelled") return true;

            // Can't change from Cancelled or Completed
            if (currentStatus == "Cancelled" || currentStatus == "Completed")
            {
                return false;
            }

            // Define valid forward transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                ["Pending Confirmation"] = new[] { "Confirmed", "Cancelled" },
                ["Confirmed"] = new[] { "Processing", "Cancelled" },
                ["Processing"] = new[] { "Shipped", "Cancelled" },
                ["Shipped"] = new[] { "Delivered", "Cancelled" },
                ["Delivered"] = new[] { "Completed", "Cancelled" }
            };

            if (validTransitions.ContainsKey(currentStatus))
            {
                return validTransitions[currentStatus].Contains(newStatus);
            }

            return false;
        }

        /// <summary>
        /// BR: Handle actions triggered by status changes
        /// </summary>
        private async Task HandleStatusChangeActions(Order order, string oldStatus, string newStatus)
        {
            // BR: Restore inventory if order is cancelled
            if (newStatus == "Cancelled" && oldStatus != "Cancelled")
            {
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
            }

            // BR: Send notifications (implement as needed)
            // TODO: Implement email/SMS notification service
        }
    }
}
