using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize(Roles = "customer,Customer")]
    public class RequestReturnModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public RequestReturnModel(EyewearStoreContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        public Order? Order { get; set; }

        // ── View Models ──────────────────────────────────────────────────────
        public class RefundItemViewModel
        {
            public int OrderItemId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string? RefundStatus { get; set; } // null = no request, "Pending", "Approved", "Rejected"
            public bool CanRequestRefund { get; set; }
        }

        public List<RefundItemViewModel> Items { get; set; } = new();
        public bool CanRefund { get; set; }
        public int DaysLeft { get; set; }

        [BindProperty]
        public int RefundOrderItemId { get; set; }

        [BindProperty]
        public string? RefundReason { get; set; }

        // ── OnGetAsync ───────────────────────────────────────────────────────

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage("/Account/Login");

            Order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Returns)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (Order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Check eligibility: must be Completed and within 7 days
            var daysSinceOrder = (DateTime.UtcNow - Order.CreatedAt).TotalDays;
            CanRefund = Order.Status == "Completed" && daysSinceOrder <= 7;
            DaysLeft = Math.Max(0, 7 - (int)daysSinceOrder);

            // Build items list with refund status
            Items = Order.OrderItems.Select(oi =>
            {
                var existingReturn = oi.Returns
                    .Where(r => r.Status == "Pending" || r.Status == "Approved")
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

                // Also check for any return at all (even rejected)
                var anyReturn = oi.Returns.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

                return new RefundItemViewModel
                {
                    OrderItemId = oi.OrderItemId,
                    ProductName = oi.Product?.Name ?? "Product",
                    Sku = oi.Product?.Sku ?? "N/A",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    RefundStatus = anyReturn?.Status,
                    // Can request only if: order is eligible, no pending/approved return exists
                    CanRequestRefund = CanRefund && existingReturn == null
                };
            }).ToList();

            return Page();
        }

        // ── OnPostAsync — Submit Refund Request ─────────────────────────────

        public async Task<IActionResult> OnPostAsync(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage("/Account/Login");

            // Validate reason
            if (string.IsNullOrWhiteSpace(RefundReason))
            {
                TempData["ErrorMessage"] = "Please provide a reason for the refund request.";
                return await OnGetAsync(orderId);
            }

            // Load order with ownership check
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Returns)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Validate order status and 7-day window
            if (order.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Refund can only be requested for completed orders.";
                return RedirectToPage("/Customer/MyOrders");
            }

            if ((DateTime.UtcNow - order.CreatedAt).TotalDays > 7)
            {
                TempData["ErrorMessage"] = "The 7-day refund window has expired for this order.";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Validate the specific order item
            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == RefundOrderItemId);
            if (orderItem == null)
            {
                TempData["ErrorMessage"] = "Invalid order item selected.";
                return await OnGetAsync(orderId);
            }

            // Block duplicate: check for existing Pending or Approved return
            var hasPendingReturn = orderItem.Returns
                .Any(r => r.Status == "Pending" || r.Status == "Approved");

            if (hasPendingReturn)
            {
                TempData["ErrorMessage"] = "A refund request already exists for this item.";
                return await OnGetAsync(orderId);
            }

            // Validate StripePaymentIntentId exists on the order
            if (string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                TempData["ErrorMessage"] = "This order does not have a valid payment reference for processing a refund. Please contact support.";
                return await OnGetAsync(orderId);
            }

            try
            {
                // Create the return/refund request
                var returnRequest = new Return
                {
                    OrderItemId = RefundOrderItemId,
                    UserId = userId,
                    Quantity = orderItem.Quantity,
                    ReturnType = "Refund",
                    ReasonCategory = "Refund Request",
                    Reason = RefundReason,
                    Description = RefundReason,
                    Status = "Pending",
                    RefundAmount = orderItem.UnitPrice * orderItem.Quantity,
                    StripePaymentIntentId = order.StripePaymentIntentId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Returns.Add(returnRequest);
                await _context.SaveChangesAsync();

                // Send email notification to sales staff
                try
                {
                    var staffEmails = await _context.Users
                        .Where(u => (u.Role == "sale" || u.Role == "sales" || u.Role == "admin") && u.IsActive)
                        .Select(u => u.Email)
                        .ToListAsync();

                    var customerName = order.User?.FullName ?? "Customer";
                    var productName = orderItem.Product?.Name ?? "Product";

                    foreach (var staffEmail in staffEmails.Take(5)) // Limit to avoid spam
                    {
                        await _emailService.SendEmailAsync(
                            staffEmail,
                            $"🔔 New Refund Request — Order #{order.OrderId}",
                            BuildStaffNotificationEmail(customerName, productName, order.OrderId, RefundReason)
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    Console.WriteLine($"Failed to send staff notification email: {ex.Message}");
                }

                TempData["SuccessMessage"] = "Refund request submitted successfully! Our team will review it within 24–48 hours.";
                return RedirectToPage("/Customer/MyOrders");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error submitting refund request: {ex.Message}";
                return await OnGetAsync(orderId);
            }
        }

        // ── Email Templates ──────────────────────────────────────────────────

        private string BuildStaffNotificationEmail(string customerName, string productName, int orderId, string? reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1a2332 0%, #2c4a6e 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 15px 0; border-left: 4px solid #ff9800; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 New Refund Request</h1>
        </div>
        <div class='content'>
            <p style='font-size: 16px; color: #333;'>A new refund request has been submitted and requires your review.</p>
            <div class='info-box'>
                <p><strong>Customer:</strong> {customerName}</p>
                <p><strong>Order:</strong> #{orderId}</p>
                <p><strong>Product:</strong> {productName}</p>
                <p><strong>Reason:</strong> {reason ?? "No reason provided"}</p>
            </div>
            <p style='color: #666; font-size: 14px;'>Please review this request in the <strong>Refund Requests</strong> management page.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} OptiPlus Eyewear. Internal notification.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
