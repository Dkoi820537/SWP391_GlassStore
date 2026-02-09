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
    /// <summary>
    /// Sale/Support Staff Order Management with pagination & search
    /// </summary>
    [Authorize(Roles = "sale,sales,support,admin,Administrator")]
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

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalOrders { get; set; }
        public int TotalPages { get; set; }

        public List<int> DisplayPageNumbers { get; set; } = new();
        private const int PageWindow = 7;

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
            // sanitize page size
            if (PageSize <= 0) PageSize = 10;
            if (PageSize > 100) PageSize = 100;

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchTerm = SearchQuery.Trim();
                if (int.TryParse(searchTerm, out var parsedId))
                {
                    query = query.Where(o =>
                        o.OrderId == parsedId ||
                        (o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm))));
                }
                else
                {
                    query = query.Where(o =>
                        (o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm))));
                }
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && AvailableStatuses.Contains(StatusFilter))
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Count total before paging
            TotalOrders = await query.CountAsync();

            // Calculate total pages and clamp
            TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            // Fetch page
            Orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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

            BuildDisplayPageNumbers();
        }

        private void BuildDisplayPageNumbers()
        {
            DisplayPageNumbers.Clear();

            if (TotalPages <= PageWindow)
            {
                for (int i = 1; i <= TotalPages; i++) DisplayPageNumbers.Add(i);
                return;
            }

            int left = Math.Max(1, PageNumber - PageWindow / 2);
            int right = Math.Min(TotalPages, left + PageWindow - 1);
            if (right - left + 1 < PageWindow) left = Math.Max(1, right - PageWindow + 1);

            for (int i = left; i <= right; i++) DisplayPageNumbers.Add(i);
        }
    }
}