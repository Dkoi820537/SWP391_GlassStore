using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context) => _context = context;

        public class OrderListItem
        {
            public int OrderId { get; set; }
            public string UserEmail { get; set; } = "";
            public string UserName { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public List<OrderListItem> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<string> AvailableStatuses { get; } = new()
        {
            "Pending Confirmation",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed",
            "Cancelled"
        };

        public async Task OnGetAsync()
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchTerm = SearchQuery.Trim().ToLower();

                // Try to parse as Order ID
                if (int.TryParse(searchTerm, out var orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        o.User.Email.ToLower().Contains(searchTerm) ||
                        o.User.FullName.ToLower().Contains(searchTerm));
                }
                else
                {
                    // Search by email or name
                    query = query.Where(o =>
                        o.User.Email.ToLower().Contains(searchTerm) ||
                        o.User.FullName.ToLower().Contains(searchTerm));
                }
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && AvailableStatuses.Contains(StatusFilter))
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Get orders with pagination (limit to 500 for performance)
            Orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(500)
                .Select(o => new OrderListItem
                {
                    OrderId = o.OrderId,
                    UserEmail = o.User.Email ?? "N/A",
                    UserName = o.User.FullName ?? "Unknown",
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }
    }
}
