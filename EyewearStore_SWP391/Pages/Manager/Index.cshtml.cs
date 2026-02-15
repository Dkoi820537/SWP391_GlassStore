using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Manager
{
    [Authorize(Roles = "manager,admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public IndexModel(EyewearStoreContext context) { _context = context; }

        public DashboardStats Stats { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public ChartDataDto ChartData { get; set; } = new();

        // Recent orders paging
        [BindProperty(SupportsGet = true)]
        public int RecentPageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int RecentPageSize { get; set; } = 10;

        public int RecentTotalOrders { get; set; }
        public int RecentTotalPages { get; set; }
        public List<int> RecentDisplayPageNumbers { get; set; } = new();
        private const int RecentPageWindow = 7;

        public class DashboardStats
        {
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int ActiveProducts { get; set; }
            public int LowStockCount { get; set; }
            public int TotalStaff { get; set; }
            public int ActiveStaff { get; set; }
            public int PendingReturns { get; set; } // ✅ ADDED
            public decimal OrderGrowth { get; set; }
        }

        public class RecentOrderDto
        {
            public int OrderId { get; set; }
            public string CustomerName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
        }

        public class TopProductDto
        {
            public string Name { get; set; } = "";
            public int SoldCount { get; set; }
            public decimal Revenue { get; set; }
        }

        public class ChartDataDto
        {
            public List<string> Labels { get; set; } = new();
            public List<decimal> Values { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            await LoadDashboardStatsAsync();
            await LoadTopProductsAsync();
            await LoadRevenueChartDataAsync();
            await LoadRecentOrdersAsync();
        }

        private async Task LoadDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);

            Stats.TotalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth && o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            Stats.TotalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .CountAsync();

            var lastMonthOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfLastMonth && o.CreatedAt <= lastDayOfLastMonth)
                .CountAsync();

            if (lastMonthOrders > 0)
                Stats.OrderGrowth = Math.Round(((decimal)(Stats.TotalOrders - lastMonthOrders) / lastMonthOrders) * 100, 1);
            else
                Stats.OrderGrowth = lastMonthOrders == 0 && Stats.TotalOrders > 0 ? 100 : 0;

            Stats.ActiveProducts = await _context.Products.CountAsync(p => p.IsActive);
            Stats.LowStockCount = await _context.Products.Where(p => p.IsActive && p.InventoryQty < 10).CountAsync();
            Stats.TotalStaff = await _context.Users.Where(u => u.Role != "customer" && u.IsActive).CountAsync();
            Stats.ActiveStaff = Stats.TotalStaff;

            // ✅ ADDED: Get pending returns count
            Stats.PendingReturns = await _context.Returns
                .Where(r => r.Status == "Pending")
                .CountAsync();
        }

        private async Task LoadTopProductsAsync()
        {
            TopProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status != "Cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                .Select(g => new TopProductDto
                {
                    Name = g.Key.Name ?? "Unknown",
                    SoldCount = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToListAsync();
        }

        private async Task LoadRevenueChartDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);
            var dailyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= sevenDaysAgo && o.Status != "Cancelled")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var dayData = dailyRevenue.FirstOrDefault(d => d.Date == date);
                ChartData.Labels.Add(date.ToString("MMM dd"));
                ChartData.Values.Add(dayData?.Revenue ?? 0);
            }
        }

        private async Task LoadRecentOrdersAsync()
        {
            var baseQuery = _context.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            RecentTotalOrders = await baseQuery.CountAsync();

            if (RecentPageSize <= 0) RecentPageSize = 10;
            if (RecentPageSize > 50) RecentPageSize = 50;

            RecentTotalPages = (int)Math.Ceiling(RecentTotalOrders / (double)RecentPageSize);
            if (RecentTotalPages < 1) RecentTotalPages = 1;
            if (RecentPageNumber < 1) RecentPageNumber = 1;
            if (RecentPageNumber > RecentTotalPages) RecentPageNumber = RecentTotalPages;

            var items = await baseQuery
                .Skip((RecentPageNumber - 1) * RecentPageSize)
                .Take(RecentPageSize)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User.FullName ?? o.User.Email ?? "Unknown",
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            RecentOrders = items;
            BuildRecentDisplayPageNumbers();
        }

        private void BuildRecentDisplayPageNumbers()
        {
            RecentDisplayPageNumbers.Clear();
            if (RecentTotalPages <= RecentPageWindow)
            {
                for (int i = 1; i <= RecentTotalPages; i++) RecentDisplayPageNumbers.Add(i);
                return;
            }

            int left = Math.Max(1, RecentPageNumber - RecentPageWindow / 2);
            int right = Math.Min(RecentTotalPages, left + RecentPageWindow - 1);
            if (right - left + 1 < RecentPageWindow) left = Math.Max(1, right - RecentPageWindow + 1);

            for (int i = left; i <= right; i++) RecentDisplayPageNumbers.Add(i);
        }
    }
}