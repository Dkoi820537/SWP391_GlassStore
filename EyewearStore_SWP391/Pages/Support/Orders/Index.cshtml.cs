using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Support.Orders
{
    /// <summary>
    /// Support Staff Order Management
    /// Default scope: Pending Confirmation + Confirmed
    /// Processing and later statuses will move to Staff dashboard
    /// </summary>
    [Authorize(Roles = "sale,admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public class OrderSummaryDto
        {
            public int OrderId { get; set; }
            public string? UserFullName { get; set; }
            public string? UserEmail { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public bool HasPrescription { get; set; }
            public bool HasReturn { get; set; }
            public bool IsLowStock { get; set; }
        }

        public List<OrderSummaryDto> Orders { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalOrders { get; set; }
        public int TotalPages { get; set; } = 1;
        public int FirstItemIndex => TotalOrders == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalOrders);

        public List<int> DisplayPageNumbers { get; set; } = new();
        private const int PageWindow = 7;

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
            public int ConfirmedCount { get; set; }
            public int PrescriptionCount { get; set; }
            public int ReturnCount { get; set; }
            public int TodayConfirmedCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (PageSize <= 0) PageSize = 10;
            if (PageSize > 100) PageSize = 100;

            await CalculateStatsAsync();

            var query = _context.Orders
                .AsNoTracking()
                .Select(o => new OrderSummaryDto
                {
                    OrderId = o.OrderId,
                    UserFullName = o.User != null ? o.User.FullName : null,
                    UserEmail = o.User != null ? o.User.Email : null,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    HasPrescription = o.OrderItems.Any(oi => oi.PrescriptionId != null),
                    HasReturn = o.OrderItems.Any(oi => oi.Returns.Any()),
                    IsLowStock = o.OrderItems.Any(oi =>
                        oi.Product != null &&
                        oi.Product.InventoryQty < oi.Quantity &&
                        (oi.SnapshotJson == null ||
                         (!oi.SnapshotJson.Contains("\"isServiceOrder\":true") &&
                          !oi.SnapshotJson.Contains("\"lensProductId\":"))))
                })
                .AsQueryable();

            // Default: only Pending Confirmation + Confirmed
            if (string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(o =>
                    o.Status == "Pending Confirmation" ||
                    o.Status == "Confirmed" ||
                    o.HasReturn);
            }
            else
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();

                if (int.TryParse(term, out var oid))
                {
                    query = query.Where(o =>
                        o.OrderId == oid ||
                        (o.UserEmail != null && o.UserEmail.Contains(term)) ||
                        (o.UserFullName != null && o.UserFullName.Contains(term)));
                }
                else
                {
                    query = query.Where(o =>
                        (o.UserEmail != null && o.UserEmail.Contains(term)) ||
                        (o.UserFullName != null && o.UserFullName.Contains(term)));
                }
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                switch (TypeFilter)
                {
                    case "Prescription":
                        query = query.Where(o => o.HasPrescription);
                        break;
                    case "ReadyStock":
                        query = query.Where(o => !o.HasPrescription && !o.IsLowStock);
                        break;
                    case "PreOrder":
                        query = query.Where(o => o.IsLowStock);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                var twoDaysAgo = DateTime.UtcNow.AddDays(-2);

                if (PriorityFilter == "High")
                {
                    query = query.Where(o =>
                        o.HasPrescription ||
                        o.HasReturn ||
                        o.CreatedAt < twoDaysAgo);
                }
                else if (PriorityFilter == "Normal")
                {
                    query = query.Where(o =>
                        !o.HasPrescription &&
                        !o.HasReturn &&
                        o.CreatedAt >= twoDaysAgo);
                }
            }

            TotalOrders = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalOrders / (double)PageSize));

            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            BuildDisplayPageNumbers();
        }

        private async Task CalculateStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            Stats.PendingCount = await _context.Orders
                .CountAsync(o => o.Status == "Pending Confirmation");

            Stats.ConfirmedCount = await _context.Orders
                .CountAsync(o => o.Status == "Confirmed");

            Stats.PrescriptionCount = await _context.Orders
                .CountAsync(o =>
                    (o.Status == "Pending Confirmation" || o.Status == "Confirmed") &&
                    o.OrderItems.Any(oi => oi.PrescriptionId != null));

            Stats.ReturnCount = await _context.Returns
                .CountAsync(r => r.Status == "Pending" || r.Status == "Under Review");

            Stats.TodayConfirmedCount = await _context.Orders
                .CountAsync(o =>
                    (o.Status == "Confirmed" || o.Status == "Processing") &&
                    o.CreatedAt >= today &&
                    o.CreatedAt < tomorrow);
        }

        public async Task<IActionResult> OnPostQuickConfirmAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            if (order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                TempData["Error"] = "Prescription orders must be verified through the detailed process.";
                return RedirectToPage("./Details", new { id = orderId });
            }

            if (order.Status != "Pending Confirmation")
            {
                TempData["Error"] = "Only pending orders can be confirmed.";
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

        private void BuildDisplayPageNumbers()
        {
            DisplayPageNumbers = new List<int>();

            int total = TotalPages;
            int current = PageNumber;

            if (total <= PageWindow)
            {
                for (int i = 1; i <= total; i++)
                    DisplayPageNumbers.Add(i);
                return;
            }

            int left = Math.Max(1, current - PageWindow / 2);
            int right = Math.Min(total, left + PageWindow - 1);

            if (right - left + 1 < PageWindow)
                left = Math.Max(1, right - PageWindow + 1);

            for (int i = left; i <= right; i++)
                DisplayPageNumbers.Add(i);
        }
    }
}