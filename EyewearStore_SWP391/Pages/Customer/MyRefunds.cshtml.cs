using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize]
    public class MyRefundsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public MyRefundsModel(EyewearStoreContext context) => _context = context;

        // ── View Model ──────────────────────────────────────────────────────

        public class RefundViewModel
        {
            public int ReturnId { get; set; }
            public int OrderId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal RefundAmount { get; set; }
            public string? Reason { get; set; }
            public string? Description { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
            public DateTime? RefundResolvedAt { get; set; }
            public string? RejectionReason { get; set; }
            public List<string> ImageUrls { get; set; } = new();
        }

        public List<RefundViewModel> Refunds { get; set; } = new();

        // ── Summary counts ──────────────────────────────────────────────────

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        // ── OnGetAsync ──────────────────────────────────────────────────────

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                Refunds = new();
                return;
            }

            var query = _context.Returns
                .Where(r => r.UserId == userId && r.ReturnType == "Refund")
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                .AsNoTracking();

            // Compute summary counts
            TotalCount = await query.CountAsync();
            PendingCount = await query.CountAsync(r => r.Status == "Pending");
            ApprovedCount = await query.CountAsync(r => r.Status == "Approved");
            RejectedCount = await query.CountAsync(r => r.Status == "Rejected");

            var refunds = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            Refunds = refunds.Select(r => new RefundViewModel
            {
                ReturnId = r.ReturnId,
                OrderId = r.OrderItem?.OrderId ?? 0,
                ProductName = r.OrderItem?.Product?.Name ?? "Product",
                Sku = r.OrderItem?.Product?.Sku ?? "N/A",
                Quantity = r.Quantity,
                RefundAmount = r.RefundAmount ?? 0,
                Reason = r.Reason ?? r.ReasonCategory ?? "",
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                RefundResolvedAt = r.RefundResolvedAt,
                RejectionReason = r.RejectionReason,
                ImageUrls = ParseImageUrls(r.ImageUrls)
            }).ToList();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static List<string> ParseImageUrls(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "badge-pending",
                "Approved" => "badge-approved",
                "Rejected" => "badge-rejected",
                _ => "badge-secondary"
            };
        }

        public static string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending" => "bi-hourglass-split",
                "Approved" => "bi-check-circle-fill",
                "Rejected" => "bi-x-circle-fill",
                _ => "bi-question-circle"
            };
        }
    }
}
