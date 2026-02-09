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
    /// Support Staff Order Management - with pagination
    /// </summary>
    [Authorize(Roles = "support,sales,sale,admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public List<Order> Orders { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        public List<string> Statuses { get; } = new()
        {
            "Pending Confirmation",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed",
            "Cancelled"
        };

        public class DashboardStats
        {
            public int PendingCount { get; set; }
            public int PrescriptionCount { get; set; }
            public int ReturnCount { get; set; }
            public int TodayConfirmedCount { get; set; }
        }

        // Pagination props
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalOrders { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        public int FirstItemIndex => (TotalOrders == 0) ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalOrders);

        public List<int> DisplayPageNumbers { get; set; } = new();
        private const int PageWindow = 7;

        public async Task OnGetAsync()
        {
            // sanitize
            if (PageSize <= 0) PageSize = 10;
            if (PageSize > 100) PageSize = 100;

            await CalculateStatsAsync();

            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Returns)
                .Include(o => o.Shipments)
                .AsQueryable();

            // Default: Show orders needing support attention
            if (string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(o =>
                    o.Status == "Pending Confirmation" ||
                    o.OrderItems.Any(oi => oi.Returns.Any(r => r.Status == "Pending")));
            }
            else
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var searchTerm = Search.Trim();
                if (int.TryParse(searchTerm, out var orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        (o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm))));
                }
                else
                {
                    query = query.Where(o =>
                        o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm)));
                }
            }

            // Apply prescription type filter at DB level if possible
            if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter == "Prescription")
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null));
            }

            // Count total BEFORE Skip/Take
            TotalOrders = await query.CountAsync();

            // Order
            query = query.OrderByDescending(o => o.CreatedAt);

            // Calculate pages and clamp PageNumber
            TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);
            if (TotalPages <= 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            // Fetch page
            var pagedOrders = await query
                .AsNoTracking()
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // For TypeFilter ReadyStock / PreOrder and for Priority filters that require in-memory checks,
            // we may need to evaluate in-memory. We'll fetch a candidate set (cap 1000) and then slice.
            bool needsInMemoryFiltering = !string.IsNullOrWhiteSpace(TypeFilter) && (TypeFilter == "ReadyStock" || TypeFilter == "PreOrder")
                                          || !string.IsNullOrWhiteSpace(PriorityFilter);

            if (needsInMemoryFiltering)
            {
                // fetch candidates (bigger set) then filter and slice in-memory
                var candidates = await query
                    .AsNoTracking()
                    .Take(1000)
                    .ToListAsync();

                // Type filters
                if (!string.IsNullOrWhiteSpace(TypeFilter))
                {
                    if (TypeFilter == "ReadyStock")
                    {
                        candidates = candidates.Where(o => !o.OrderItems.Any(oi => oi.PrescriptionId != null)
                                                           && o.OrderItems.All(oi => (oi.Product?.InventoryQty ?? 0) >= oi.Quantity))
                                               .ToList();
                    }
                    else if (TypeFilter == "PreOrder")
                    {
                        candidates = candidates.Where(o => o.OrderItems.Any(oi => (oi.Product?.InventoryQty ?? 0) < oi.Quantity)).ToList();
                    }
                }

                // Priority filter
                if (!string.IsNullOrWhiteSpace(PriorityFilter))
                {
                    if (PriorityFilter == "High")
                    {
                        candidates = candidates.Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null)
                                                           || o.CreatedAt < DateTime.UtcNow.AddDays(-2)
                                                           || o.OrderItems.Any(oi => oi.Returns.Any()))
                                               .ToList();
                    }
                    else if (PriorityFilter == "Normal")
                    {
                        candidates = candidates.Where(o => !o.OrderItems.Any(oi => oi.PrescriptionId != null)
                                                           && o.CreatedAt >= DateTime.UtcNow.AddDays(-2)
                                                           && !o.OrderItems.Any(oi => oi.Returns.Any()))
                                               .ToList();
                    }
                }

                // recalc totals and paging based on filtered candidates
                TotalOrders = candidates.Count;
                TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);
                if (TotalPages <= 0) TotalPages = 1;
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                Orders = candidates
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            else
            {
                Orders = pagedOrders;
            }

            BuildDisplayPageNumbers();
        }

        private void BuildDisplayPageNumbers()
        {
            DisplayPageNumbers = new List<int>();
            int total = TotalPages;
            int current = PageNumber;

            if (total <= PageWindow)
            {
                for (int i = 1; i <= total; i++) DisplayPageNumbers.Add(i);
                return;
            }

            int left = Math.Max(1, current - PageWindow / 2);
            int right = Math.Min(total, left + PageWindow - 1);

            if (right - left + 1 < PageWindow)
            {
                left = Math.Max(1, right - PageWindow + 1);
            }

            for (int i = left; i <= right; i++) DisplayPageNumbers.Add(i);
        }

        private async Task CalculateStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            Stats.PendingCount = await _context.Orders.CountAsync(o => o.Status == "Pending Confirmation");

            Stats.PrescriptionCount = await _context.Orders
                .Where(o => o.Status == "Pending Confirmation" || o.Status == "Confirmed")
                .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null))
                .CountAsync();

            Stats.ReturnCount = await _context.Returns
                .Where(r => r.Status == "Pending" || r.Status == "Under Review")
                .CountAsync();

            Stats.TodayConfirmedCount = await _context.Orders
                .Where(o => o.Status == "Confirmed" || o.Status == "Processing" || o.Status == "Shipped")
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .CountAsync();
        }

        public async Task<IActionResult> OnPostQuickConfirmAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                TempData["Error"] = "Prescription orders must be verified through the detailed process.";
                return RedirectToPage("./Details", new { id = orderId });
            }

            if (order.Status != "Pending Confirmation")
            {
                TempData["Error"] = "Only pending orders can be confirmed.";
                return RedirectToPage();
            }

            order.Status = "Confirmed";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} confirmed successfully!";
            return RedirectToPage(new
            {
                pageNumber = PageNumber,
                pageSize = PageSize,
                Search,
                StatusFilter,
                TypeFilter,
                PriorityFilter
            });
        }
    }
}