using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Manager
{
    [Authorize(Roles = "manager,Manager,admin,Admin")]
    public class ReturnsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public ReturnsModel(EyewearStoreContext context) => _context = context;

        public class ReturnItemViewModel
        {
            public int ReturnId { get; set; }
            public int OrderId { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal RefundAmount { get; set; }
            public string ReturnType { get; set; } = "";
            public string ReasonCategory { get; set; } = "";
            public string? Description { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int DaysAgo { get; set; }
            public List<string> ImageUrls { get; set; } = new();
        }

        public List<ReturnItemViewModel> Returns { get; set; } = new();
        public string CurrentFilter { get; set; } = "Pending";
        public int TotalReturns { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

        public async Task OnGetAsync(string? filter)
        {
            CurrentFilter = filter ?? "Pending";

            var query = _context.Returns
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                        .ThenInclude(o => o.User)
                .AsNoTracking();

            // Get counts
            TotalReturns = await query.CountAsync();
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

            Returns = returns.Select(r => new ReturnItemViewModel
            {
                ReturnId = r.ReturnId,
                OrderId = r.OrderItem.OrderId,
                CustomerName = r.OrderItem.Order.User?.FullName ?? "Customer",
                CustomerEmail = r.OrderItem.Order.User?.Email ?? "",
                ProductName = r.OrderItem.Product?.Name ?? "Product",
                Sku = r.OrderItem.Product?.Sku ?? "N/A",
                Quantity = r.Quantity,
                RefundAmount = r.RefundAmount ?? 0,
                ReturnType = r.ReturnType ?? "Refund",
                ReasonCategory = r.ReasonCategory ?? "",
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                DaysAgo = (int)(DateTime.UtcNow - r.CreatedAt).TotalDays,
                ImageUrls = ParseImageUrls(r.ImageUrls)
            }).ToList();
        }

        public async Task<IActionResult> OnPostQuickApproveAsync(int returnId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var returnRequest = await _context.Returns.FindAsync(returnId);
            if (returnRequest == null)
            {
                TempData["ErrorMessage"] = "Return not found!";
                return RedirectToPage();
            }

            returnRequest.Status = "Approved";
            returnRequest.ReviewedBy = userId;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return #{returnId} approved successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostQuickRejectAsync(int returnId, string reason)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var returnRequest = await _context.Returns.FindAsync(returnId);
            if (returnRequest == null)
            {
                TempData["ErrorMessage"] = "Return not found!";
                return RedirectToPage();
            }

            returnRequest.Status = "Rejected";
            returnRequest.ReviewedBy = userId;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.RejectionReason = reason;
            returnRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Return #{returnId} rejected.";
            return RedirectToPage();
        }

        private List<string> ParseImageUrls(string? json)
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
    }
}
