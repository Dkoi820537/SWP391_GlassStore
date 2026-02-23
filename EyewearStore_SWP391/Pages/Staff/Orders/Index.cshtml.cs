using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EyewearStore_SWP391.Pages.Staff.Orders
{
    /// <summary>
    /// Operations Staff Order Management - with pagination
    /// </summary>
    [Authorize(Roles = "staff,operational,admin,Administrator,support")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        // Results for current page
        public List<Order> Orders { get; set; } = new();

        // Dashboard stats (unchanged)
        public DashboardStats Stats { get; set; } = new();

        // Filters (keep as before)
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
            public int ProcessingCount { get; set; }
            public int PrescriptionCount { get; set; }
            public int ShippedTodayCount { get; set; }
        }

        // ---------- Pagination properties ----------
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalOrders { get; set; } = 0;
        public int TotalPages { get; set; } = 1;

        // Friendly indices for UI "Showing X - Y of Z"
        public int FirstItemIndex => (TotalOrders == 0) ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalOrders);

        // For UI: which page numbers to display (compact window)
        public List<int> DisplayPageNumbers { get; set; } = new();

        // Maximum page links to show
        private const int PageWindow = 7;

        // ---------- End pagination properties ----------

        public async Task OnGetAsync()
        {
            // sanitize page size
            if (PageSize <= 0) PageSize = 10;
            if (PageSize > 100) PageSize = 100; // safety cap

            await CalculateStatsAsync();

            // Build base query
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Shipments)
                .AsQueryable();

            // Default: Show orders needing operations attention when no status filter
            if (string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(o =>
                    o.Status == "Confirmed" ||
                    o.Status == "Processing" ||
                    o.Status == "Shipped");
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

            // Count total (before applying Skip/Take)
            TotalOrders = await query.CountAsync();

            // Apply ordering
            query = query.OrderByDescending(o => o.CreatedAt);

            // Type Filter (in-memory previously) - we can still do in-memory after fetching page or filter before counting.
            // Simpler: apply type filter by narrowing via Where where possible.
            // However since TypeFilter logic can be complex (inventory checks), we will:
            // - If TypeFilter == "Prescription" we can narrow via DB: any orderitems with prescriptionId != null
            // - For ReadyStock / PreOrder, we need product inventory values; we'll filter in-memory after fetching a bit extra.
            if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter == "Prescription")
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null));
                // recalc total
                TotalOrders = await query.CountAsync();
            }

            // Adjust PageNumber bounds
            TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);
            if (TotalPages <= 0) TotalPages = 1;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            // Fetch the page (use Skip/Take)
            var pagedOrders = await query
                .AsNoTracking()
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // For TypeFilter that requires in-memory evaluation (ReadyStock / PreOrder), apply now.
            if (!string.IsNullOrWhiteSpace(TypeFilter) && (TypeFilter == "ReadyStock" || TypeFilter == "PreOrder"))
            {
                // We'll fetch enough items to fill current page after filtering.
                // To ensure we don't show empty pages, we may need to fetch more results.
                // Simple approach: fetch up to 500 matching records then apply in-memory Skip/Take.
                var sourceQuery = query; // current query (already filtered by status/search)
                var allCandidates = await sourceQuery
                    .AsNoTracking()
                    .Take(500) // safety cap
                    .ToListAsync();

                if (TypeFilter == "ReadyStock")
                {
                    allCandidates = allCandidates
                        .Where(o => !o.OrderItems.Any(oi => oi.PrescriptionId != null) &&
                                    o.OrderItems.All(oi => (oi.Product?.InventoryQty ?? 0) >= oi.Quantity))
                        .ToList();
                }
                else // PreOrder
                {
                    allCandidates = allCandidates
                        .Where(o => o.OrderItems.Any(oi => (oi.Product?.InventoryQty ?? 0) < oi.Quantity))
                        .ToList();
                }

                // update total and pages based on filtered list
                TotalOrders = allCandidates.Count;
                TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);
                if (TotalPages <= 0) TotalPages = 1;
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                // take page slice in-memory
                Orders = allCandidates
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            else
            {
                // Use pagedOrders result
                Orders = pagedOrders;
            }

            // Build DisplayPageNumbers (compact window)
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
            Stats.ProcessingCount = await _context.Orders.CountAsync(o => o.Status == "Confirmed" || o.Status == "Processing");

            Stats.PrescriptionCount = await _context.Orders
                .Where(o => o.Status != "Completed" && o.Status != "Cancelled")
                .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null))
                .CountAsync();

            Stats.ShippedTodayCount = await _context.Shipments
                .Where(s => s.ShippedAt != null && s.ShippedAt >= today && s.ShippedAt < tomorrow)
                .CountAsync();
        }

        // Quick confirm handler (unchanged)
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