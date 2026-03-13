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
    [Authorize(Roles = "staff,operations,admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public IndexModel(EyewearStoreContext context) => _context = context;

        public class OrderSummaryDto
        {
            public int OrderId { get; set; }
            public string? UserFullName { get; set; }
            public string? UserEmail { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public bool HasPrescription { get; set; }
            public bool IsPreOrder { get; set; }
            public string? TrackingNumber { get; set; }
            public string? Carrier { get; set; }
        }

        public List<OrderSummaryDto> Orders { get; set; } = new();
        public OpsStats Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? TypeFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? PriorityFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 10;

        public int TotalOrders { get; set; }
        public int TotalPages { get; set; } = 1;
        public int FirstItemIndex => TotalOrders == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
        public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalOrders);
        public List<int> DisplayPageNumbers { get; set; } = new();
        private const int PageWindow = 7;

        public List<string> Statuses { get; } = new()
        {
            "Processing", "Shipped", "Delivered", "Completed", "Cancelled"
        };

        // ServiceCount removed — service jobs moved to Support role
        public class OpsStats
        {
            public int ProcessingCount { get; set; }
            public int PrescriptionCount { get; set; }
            public int ShippedTodayCount { get; set; }
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
                    IsPreOrder = o.OrderItems.Any(oi =>
                        oi.Product != null && oi.Product.InventoryQty < oi.Quantity),
                    TrackingNumber = o.Shipments.Any()
                        ? o.Shipments.OrderByDescending(s => s.ShipmentId).Select(s => s.TrackingNumber).FirstOrDefault()
                        : null,
                    Carrier = o.Shipments.Any()
                        ? o.Shipments.OrderByDescending(s => s.ShipmentId).Select(s => s.Carrier).FirstOrDefault()
                        : null,
                })
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(StatusFilter))
                query = query.Where(o => o.Status == "Processing" || o.Status == "Shipped");
            else
                query = query.Where(o => o.Status == StatusFilter);

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                if (int.TryParse(term, out var oid))
                    query = query.Where(o => o.OrderId == oid ||
                        (o.UserEmail != null && o.UserEmail.Contains(term)) ||
                        (o.UserFullName != null && o.UserFullName.Contains(term)));
                else
                    query = query.Where(o =>
                        (o.UserEmail != null && o.UserEmail.Contains(term)) ||
                        (o.UserFullName != null && o.UserFullName.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                switch (TypeFilter)
                {
                    case "Prescription":
                        query = query.Where(o => o.HasPrescription); break;
                    case "PreOrder":
                        query = query.Where(o => o.IsPreOrder); break;
                    case "Standard":
                        query = query.Where(o => !o.HasPrescription && !o.IsPreOrder); break;
                }
            }

            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                var twoDaysAgo = DateTime.UtcNow.AddDays(-2);
                if (PriorityFilter == "High")
                    query = query.Where(o => o.HasPrescription || o.CreatedAt < twoDaysAgo);
                else if (PriorityFilter == "Normal")
                    query = query.Where(o => !o.HasPrescription && o.CreatedAt >= twoDaysAgo);
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

            Stats.ProcessingCount = await _context.Orders
                .CountAsync(o => o.Status == "Processing");

            Stats.PrescriptionCount = await _context.Orders
                .CountAsync(o => (o.Status == "Processing" || o.Status == "Shipped")
                              && o.OrderItems.Any(oi => oi.PrescriptionId != null));

            Stats.ShippedTodayCount = await _context.Orders
                .CountAsync(o => o.Status == "Shipped"
                              && o.CreatedAt >= today && o.CreatedAt < tomorrow);
            // ServiceCount removed — service orders belong to Support role
        }

        private void BuildDisplayPageNumbers()
        {
            DisplayPageNumbers = new List<int>();
            int total = TotalPages, current = PageNumber;
            if (total <= PageWindow)
            {
                for (int i = 1; i <= total; i++) DisplayPageNumbers.Add(i);
                return;
            }
            int left = Math.Max(1, current - PageWindow / 2);
            int right = Math.Min(total, left + PageWindow - 1);
            if (right - left + 1 < PageWindow) left = Math.Max(1, right - PageWindow + 1);
            for (int i = left; i <= right; i++) DisplayPageNumbers.Add(i);
        }
    }
}
