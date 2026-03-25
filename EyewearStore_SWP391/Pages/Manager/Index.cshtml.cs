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
            try { await LoadDashboardStatsAsync(); } catch { }
            try { await LoadTopProductsAsync(); } catch { }
            try { await LoadRevenueChartDataAsync(); } catch { }
            try { await LoadRecentOrdersAsync(); } catch { }
        }

        private async Task LoadDashboardStatsAsync()
        {
            // Dùng giờ Việt Nam UTC+7
            var now = DateTime.UtcNow.AddHours(7);
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            // ✅ SỬA: Revenue = tất cả đơn Completed (không giới hạn tháng tạo)
            // Lý do: đơn tạo tháng trước nhưng Completed tháng này vẫn phải tính
            // Nếu muốn lọc theo tháng, dùng UpdatedAt thay CreatedAt
            // Hiện tại lấy toàn bộ đơn Completed trong tháng (kể cả tạo tháng trước)
            Stats.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed"
                         && o.CreatedAt >= firstDayOfMonth
                         && o.CreatedAt < firstDayOfNextMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ✅ THÊM: Nếu bạn có cột UpdatedAt/CompletedAt trong Orders, dùng cách này tốt hơn:
            // Stats.TotalRevenue = await _context.Orders
            //     .Where(o => o.Status == "Completed"
            //              && o.UpdatedAt >= firstDayOfMonth
            //              && o.UpdatedAt < firstDayOfNextMonth)
            //     .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ✅ SỬA: TotalOrders = tất cả đơn trong tháng, mọi trạng thái
            Stats.TotalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfMonth
                         && o.CreatedAt < firstDayOfNextMonth)
                .CountAsync();

            // Tháng trước để tính tăng trưởng
            var lastMonthOrders = await _context.Orders
                .Where(o => o.CreatedAt >= firstDayOfLastMonth
                         && o.CreatedAt < firstDayOfMonth)
                .CountAsync();

            Stats.OrderGrowth = lastMonthOrders > 0
                ? Math.Round(((decimal)(Stats.TotalOrders - lastMonthOrders) / lastMonthOrders) * 100, 1)
                : (Stats.TotalOrders > 0 ? 100 : 0);

            Stats.ActiveProducts = await _context.Products.CountAsync(p => p.IsActive);
            Stats.LowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.InventoryQty < 10);
            Stats.TotalStaff = await _context.Users.CountAsync(u => u.Role != "customer" && u.IsActive);
            Stats.ActiveStaff = Stats.TotalStaff;

            Stats.PendingReturns = await _context.Returns
                .CountAsync(r => r.Status == "Pending");
        }

        private async Task LoadTopProductsAsync()
        {
            // ✅ SỬA: Chỉ tính đơn Completed (đã hoàn thành thực sự)
            // Trước đây loại Cancelled + Pending nhưng vẫn tính cả Processing/Shipped chưa xong
            TopProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == "Completed")
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
            var nowLocal = DateTime.UtcNow.AddHours(7);
            var todayLocal = nowLocal.Date;
            var sevenDaysAgo = todayLocal.AddDays(-6);

            // ✅ SỬA: Chỉ lấy đơn Completed cho biểu đồ doanh thu
            // Trước đây loại Cancelled + Pending nhưng vẫn tính đơn đang xử lý
            var dailyRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= sevenDaysAgo
                         && o.Status == "Completed")
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
            // ✅ SỬA: Hiển thị tất cả đơn, không lọc theo trạng thái
            // Manager cần thấy toàn bộ lịch sử, kể cả Completed
            var baseQuery = _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt);

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
                    CustomerName = o.User != null
                        ? (o.User.FullName != null && o.User.FullName != ""
                            ? o.User.FullName
                            : o.User.Email ?? "Unknown")
                        : "Unknown",
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
            if (right - left + 1 < RecentPageWindow)
                left = Math.Max(1, right - RecentPageWindow + 1);

            for (int i = left; i <= right; i++) RecentDisplayPageNumbers.Add(i);
        }
    }
}