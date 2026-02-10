using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace EyewearStore_SWP391.Pages.Manager
{
    /// <summary>
    /// Manager Dashboard - Overview of business operations
    /// Role: Manager can view analytics, manage policies, staff, and products
    /// </summary>
    [Authorize(Roles = "manager,admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public DashboardStats Stats { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public ChartDataDto ChartData { get; set; } = new();

        public class DashboardStats
        {
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int ActiveProducts { get; set; }
            public int LowStockCount { get; set; }
            public int TotalStaff { get; set; }
            public int ActiveStaff { get; set; }
            public int PendingReturns { get; set; }
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
            await LoadRecentOrdersAsync();
            await LoadTopProductsAsync();
            await LoadRevenueChartDataAsync();
        }

        private async Task LoadDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
            var lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);

            // Total Revenue (This Month)
            Stats.TotalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalAmount);

            // Total Orders (This Month)
            Stats.TotalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .CountAsync();

            // Last Month Orders (for growth calculation)
            var lastMonthOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfLastMonth && o.CreatedAt <= lastDayOfLastMonth)
                .CountAsync();

            // Calculate growth percentage
            if (lastMonthOrders > 0)
            {
                Stats.OrderGrowth = Math.Round(((decimal)(Stats.TotalOrders - lastMonthOrders) / lastMonthOrders) * 100, 1);
            }

            // Active Products
            Stats.ActiveProducts = await _context.Products
                .CountAsync(p => p.IsActive);

            // Low Stock Count (InventoryQty < 10)
            Stats.LowStockCount = await _context.Products
                .Where(p => p.IsActive && p.InventoryQty < 10)
                .CountAsync();

            // Total Staff
            Stats.TotalStaff = await _context.Users
                .Where(u => u.Role != "customer" && u.IsActive)
                .CountAsync();

            // Active Staff (logged in today - simplified)
            Stats.ActiveStaff = Stats.TotalStaff; // TODO: Track last login time

            // Pending Returns
            Stats.PendingReturns = await _context.Returns
                .Where(r => r.Status == "Pending" || r.Status == "Under Review")
                .CountAsync();
        }

        private async Task LoadRecentOrdersAsync()
        {
            RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User.FullName ?? o.User.Email ?? "Unknown",
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();
        }

        private async Task LoadTopProductsAsync()
        {
            // Get top 5 products by revenue (from OrderItems)
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
            // Last 7 days revenue
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);

            var dailyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= sevenDaysAgo && o.Status != "Cancelled")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill in missing dates with 0 revenue
            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                var dayData = dailyRevenue.FirstOrDefault(d => d.Date == date);

                ChartData.Labels.Add(date.ToString("MMM dd"));
                ChartData.Values.Add(dayData?.Revenue ?? 0);
            }
        }
    }
}
