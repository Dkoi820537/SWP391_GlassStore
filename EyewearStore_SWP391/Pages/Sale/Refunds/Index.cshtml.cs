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
using Stripe;

namespace EyewearStore_SWP391.Pages.Sale.Refunds
{
    [Authorize(Roles = "sale,sales,support,admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(EyewearStoreContext context, IEmailService emailService, ILogger<IndexModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // ── View Models ──────────────────────────────────────────────────────

        public class RefundRequestItem
        {
            public int ReturnId { get; set; }
            public int OrderId { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal RefundAmount { get; set; }
            public string? Reason { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int DaysAgo { get; set; }
            public string? StripePaymentIntentId { get; set; }
            public DateTime? RefundResolvedAt { get; set; }
        }

        public List<RefundRequestItem> RefundRequests { get; set; } = new();
        public string CurrentFilter { get; set; } = "Pending";
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        // ── OnGetAsync ───────────────────────────────────────────────────────

        public async Task OnGetAsync(string? filter)
        {
            CurrentFilter = filter ?? "Pending";

            var query = _context.Returns
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.User)
                .Where(r => r.ReturnType == "Refund")
                .AsNoTracking();

            // Get counts
            TotalCount = await query.CountAsync();
            PendingCount = await query.CountAsync(r => r.Status == "Pending");
            ApprovedCount = await query.CountAsync(r => r.Status == "Approved");
            RejectedCount = await query.CountAsync(r => r.Status == "Rejected");

            // Apply filter
            if (!string.IsNullOrEmpty(CurrentFilter) && CurrentFilter != "All")
            {
                query = query.Where(r => r.Status == CurrentFilter);
            }

            var returns = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            RefundRequests = returns.Select(r => new RefundRequestItem
            {
                ReturnId = r.ReturnId,
                OrderId = r.OrderItem?.OrderId ?? 0,
                CustomerName = r.OrderItem?.Order?.User?.FullName ?? "Customer",
                CustomerEmail = r.OrderItem?.Order?.User?.Email ?? "",
                ProductName = r.OrderItem?.Product?.Name ?? "Product",
                Sku = r.OrderItem?.Product?.Sku ?? "N/A",
                Quantity = r.Quantity,
                RefundAmount = r.RefundAmount ?? 0,
                Reason = r.Reason ?? r.Description ?? "",
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                DaysAgo = (int)(DateTime.UtcNow - r.CreatedAt).TotalDays,
                StripePaymentIntentId = r.StripePaymentIntentId,
                RefundResolvedAt = r.RefundResolvedAt
            }).ToList();
        }

        // ── OnPostApproveAsync ───────────────────────────────────────────────

        public async Task<IActionResult> OnPostApproveAsync(int returnId)
        {
            var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var returnRequest = await _context.Returns
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.User)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId);

            if (returnRequest == null)
            {
                TempData["ErrorMessage"] = "Refund request not found!";
                return RedirectToPage();
            }

            if (returnRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToPage();
            }

            // Validate StripePaymentIntentId
            var paymentIntentId = returnRequest.StripePaymentIntentId;
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                TempData["ErrorMessage"] = "No Stripe Payment Intent ID found for this refund. Cannot process through Stripe.";
                return RedirectToPage();
            }

            // Call Stripe Refund API
            try
            {
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                };
                await refundService.CreateAsync(refundOptions);

                _logger.LogInformation("Stripe refund created for Return #{ReturnId}, PaymentIntent: {PaymentIntentId}",
                    returnId, paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe refund failed for Return #{ReturnId}", returnId);
                TempData["ErrorMessage"] = $"Stripe refund failed: {ex.Message}. Please try again or process manually.";
                return RedirectToPage();
            }

            // Update return status
            returnRequest.Status = "Approved";
            returnRequest.ReviewedBy = staffUserId;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.RefundResolvedAt = DateTime.UtcNow;
            returnRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send approval email to customer
            try
            {
                var customerEmail = returnRequest.OrderItem?.Order?.User?.Email;
                var customerName = returnRequest.OrderItem?.Order?.User?.FullName ?? "Customer";
                var productName = returnRequest.OrderItem?.Product?.Name ?? "Product";
                var orderId = returnRequest.OrderItem?.OrderId ?? 0;

                if (!string.IsNullOrEmpty(customerEmail))
                {
                    await _emailService.SendEmailAsync(
                        customerEmail,
                        $"Your Refund Has Been Approved — Order #{orderId}",
                        BuildApprovalEmail(customerName, productName, orderId, returnRequest.RefundAmount ?? 0)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send refund approval email for Return #{ReturnId}", returnId);
            }

            TempData["SuccessMessage"] = $"Refund #{returnId} approved! Stripe refund has been initiated.";
            return RedirectToPage();
        }

        // ── OnPostRejectAsync ────────────────────────────────────────────────

        public async Task<IActionResult> OnPostRejectAsync(int returnId, string? rejectionReason)
        {
            var staffUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var returnRequest = await _context.Returns
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.User)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId);

            if (returnRequest == null)
            {
                TempData["ErrorMessage"] = "Refund request not found!";
                return RedirectToPage();
            }

            if (returnRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return RedirectToPage();
            }

            // Update return status
            returnRequest.Status = "Rejected";
            returnRequest.ReviewedBy = staffUserId;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.RefundResolvedAt = DateTime.UtcNow;
            returnRequest.RejectionReason = rejectionReason ?? "No reason provided";
            returnRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send rejection email to customer
            try
            {
                var customerEmail = returnRequest.OrderItem?.Order?.User?.Email;
                var customerName = returnRequest.OrderItem?.Order?.User?.FullName ?? "Customer";
                var productName = returnRequest.OrderItem?.Product?.Name ?? "Product";
                var orderId = returnRequest.OrderItem?.OrderId ?? 0;

                if (!string.IsNullOrEmpty(customerEmail))
                {
                    await _emailService.SendEmailAsync(
                        customerEmail,
                        $"Refund Request Update — Order #{orderId}",
                        BuildRejectionEmail(customerName, productName, orderId, rejectionReason)
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send rejection email for Return #{ReturnId}", returnId);
            }

            TempData["SuccessMessage"] = $"Refund #{returnId} has been rejected.";
            return RedirectToPage();
        }

        // ── Email Templates ──────────────────────────────────────────────────

        private string BuildApprovalEmail(string customerName, string productName, int orderId, decimal refundAmount)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #4caf50 0%, #66bb6a 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #f1f8e9; border-radius: 8px; padding: 20px; margin: 15px 0; border-left: 4px solid #4caf50; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Refund Approved</h1>
        </div>
        <div class='content'>
            <h2 style='color: #333; margin-bottom: 10px;'>Great news, {customerName}!</h2>
            <p style='font-size: 16px; color: #555;'>Your refund request has been approved and processed.</p>
            <div class='info-box'>
                <p><strong>Order:</strong> #{orderId}</p>
                <p><strong>Product:</strong> {productName}</p>
                <p><strong>Refund Amount:</strong> {refundAmount:N0} VND</p>
            </div>
            <p style='color: #666; font-size: 14px;'>
                The refund will appear in your account within <strong>5–10 business days</strong>, depending on your payment provider.
            </p>
            <p style='color: #666; font-size: 14px;'>
                If you have any questions, please don't hesitate to contact our support team.
            </p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} OptiPlus Eyewear. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildRejectionEmail(string customerName, string productName, int orderId, string? reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #f44336 0%, #ef5350 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #fbe9e7; border-radius: 8px; padding: 20px; margin: 15px 0; border-left: 4px solid #f44336; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 13px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Refund Request Update</h1>
        </div>
        <div class='content'>
            <p style='font-size: 16px; color: #555;'>Dear {customerName},</p>
            <p style='font-size: 16px; color: #555;'>We've reviewed your refund request and unfortunately, we're unable to process it at this time.</p>
            <div class='info-box'>
                <p><strong>Order:</strong> #{orderId}</p>
                <p><strong>Product:</strong> {productName}</p>
                <p><strong>Reason:</strong> {reason ?? "Please contact our support team for more details."}</p>
            </div>
            <p style='color: #666; font-size: 14px;'>
                If you have questions, please <strong>contact our support team</strong> and we'll be happy to assist you.
            </p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} OptiPlus Eyewear. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
