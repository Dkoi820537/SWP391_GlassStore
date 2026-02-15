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
    [Authorize(Roles = "customer,Customer")]
    public class MyReturnsModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public MyReturnsModel(EyewearStoreContext context) => _context = context;

        public class ReturnViewModel
        {
            public int ReturnId { get; set; }
            public int OrderId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal RefundAmount { get; set; }
            public string ReturnType { get; set; } = "";
            public string ReasonCategory { get; set; } = "";
            public string? Description { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
            public string? RejectionReason { get; set; }
            public List<string> ImageUrls { get; set; } = new();
        }

        public List<ReturnViewModel> Returns { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                Returns = new();
                return;
            }

            var returns = await _context.Returns
                .Where(r => r.UserId == userId)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Order)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            Returns = returns.Select(r => new ReturnViewModel
            {
                ReturnId = r.ReturnId,
                OrderId = r.OrderItem.OrderId,
                ProductName = r.OrderItem.Product?.Name ?? "Product",
                Sku = r.OrderItem.Product?.Sku ?? "N/A",
                Quantity = r.Quantity,
                RefundAmount = r.RefundAmount ?? 0,
                ReturnType = r.ReturnType ?? "Refund",
                ReasonCategory = r.ReasonCategory ?? "",
                Description = r.Description,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                RejectionReason = r.RejectionReason,
                ImageUrls = ParseImageUrls(r.ImageUrls)
            }).ToList();
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

        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "bg-warning text-dark",
                "Approved" => "bg-success",
                "Rejected" => "bg-danger",
                "Product Received" => "bg-info",
                "Refunded" => "bg-success",
                "Completed" => "bg-success",
                _ => "bg-secondary"
            };
        }

        public static string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending" => "bi-hourglass-split",
                "Approved" => "bi-check-circle",
                "Rejected" => "bi-x-circle",
                "Product Received" => "bi-box-seam",
                "Refunded" => "bi-cash-coin",
                "Completed" => "bi-check-all",
                _ => "bi-question-circle"
            };
        }
    }
}
