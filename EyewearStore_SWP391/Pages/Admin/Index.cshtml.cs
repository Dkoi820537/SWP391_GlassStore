using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _db;
        public IndexModel(EyewearStoreContext db) => _db = db;

        public bool IsAdmin => User.IsInRole("admin");

        // ── Order stats ───────────────────────────────────────────────────────
        public int OrderTotal { get; set; }
        public int OrderPending { get; set; }
        public int OrderToday { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenueToday { get; set; }

        // ── Revenue breakdown (chart) ─────────────────────────────────────────
        public decimal RevenueFrame { get; set; }
        public decimal RevenueLens { get; set; }
        public decimal RevenueService { get; set; }
        public List<decimal> RevenueBreakdownValues { get; set; } = new();
        public List<string> RevenueBreakdownLabels { get; set; } = new();

        // ── Service order stats ───────────────────────────────────────────────
        public int SvcTotal { get; set; }
        public int SvcPending { get; set; }
        public int SvcProcessing { get; set; }
        public int SvcReady { get; set; }

        // ── Appointment stats ────────────────────────────────────────────────
        public int ApptTotal { get; set; }
        public int ApptPending { get; set; }
        public int ApptToday { get; set; }
        public int ApptConfirmed { get; set; }

        // ── Alerts ────────────────────────────────────────────────────────────
        public List<AlertItem> Alerts { get; set; } = new();

        // ── Recent data ───────────────────────────────────────────────────────
        public List<RecentOrder> RecentOrders { get; set; } = new();
        public List<EyeExamAppointment> UpcomingAppts { get; set; } = new();
        public List<SvcOrderRow> PendingSvcOrders { get; set; } = new();

        // ── Chart data ────────────────────────────────────────────────────────
        public List<decimal> RevenueByDay { get; set; } = new();
        public List<string> RevenueByDayLabels { get; set; } = new();
        public List<decimal> ServiceRevenueByDay { get; set; } = new();
        public List<decimal> TopProducts { get; set; } = new();
        public List<string> TopProductNames { get; set; } = new();
        public string TodayVsYesterdayPct { get; set; } = "0";

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);
            var sevenDaysAgo = today.AddDays(-6);

            // ── Orders ───────────────────────────────────────────────────────
            var orders = _db.Orders.AsQueryable();

            OrderTotal = await orders.CountAsync();
            OrderPending = await orders.CountAsync(o => o.Status == "Pending" || o.Status == "Processing");
            OrderToday = await orders.CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);

            if (IsAdmin)
            {
                Revenue = await orders
                    .Where(o => o.Status != "Cancelled")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                RevenueToday = await orders
                    .Where(o => o.Status != "Cancelled" && o.CreatedAt >= today && o.CreatedAt < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                // ── Revenue by day (7 days) ───────────────────────────────────
                var dailyRevenue = await _db.Orders
                    .Where(o => o.Status != "Cancelled"
                             && o.CreatedAt >= sevenDaysAgo
                             && o.CreatedAt < tomorrow)
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(o => (decimal?)o.TotalAmount) ?? 0m
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                RevenueByDay.Clear();
                RevenueByDayLabels.Clear();

                for (int i = 6; i >= 0; i--)
                {
                    var d = today.AddDays(-i);
                    var found = dailyRevenue.FirstOrDefault(x => x.Date == d);

                    RevenueByDay.Add(found?.Total ?? 0m);
                    RevenueByDayLabels.Add(d.ToString("dd/MM"));
                }

                // ── Service revenue by day (7 days) ───────────────────────────
                var dailyServiceRevenue = await (
                    from i in _db.OrderItems
                    join o in _db.Orders on i.OrderId equals o.OrderId
                    where i.SnapshotJson != null
                       && (i.SnapshotJson.Contains("\"isServiceOrder\":true")
                        || i.SnapshotJson.Contains("\"lensProductId\":"))
                       && o.Status != "Cancelled"
                       && o.CreatedAt >= sevenDaysAgo
                       && o.CreatedAt < tomorrow
                    group i by o.CreatedAt.Date into g
                    select new
                    {
                        Date = g.Key,
                        Total = g.Sum(x => (decimal?)(x.UnitPrice * x.Quantity)) ?? 0m
                    }
                )
                .OrderBy(x => x.Date)
                .ToListAsync();

                ServiceRevenueByDay.Clear();

                for (int i = 6; i >= 0; i--)
                {
                    var d = today.AddDays(-i);
                    var found = dailyServiceRevenue.FirstOrDefault(x => x.Date == d);

                    ServiceRevenueByDay.Add(found?.Total ?? 0m);
                }

                // ── Today vs yesterday ───────────────────────────────────────
                var revenueYesterday = await _db.Orders
                    .Where(o => o.Status != "Cancelled" && o.CreatedAt >= yesterday && o.CreatedAt < today)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                if (revenueYesterday > 0)
                {
                    var pct = (RevenueToday - revenueYesterday) / revenueYesterday * 100m;
                    TodayVsYesterdayPct = pct.ToString("F1", CultureInfo.InvariantCulture);
                }
                else if (RevenueToday > 0)
                {
                    TodayVsYesterdayPct = "100";
                }

                // ── Load order items with product info ────────────────────────
                var orderItems = await _db.OrderItems
                    .Include(i => i.Product)
                    .Where(i => i.Product != null)
                    .ToListAsync();

                // ── Top 5 products by revenue ─────────────────────────────────
                var topProducts = orderItems
                    .Where(i => !IsServiceOrderItem(i))
                    .GroupBy(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product!.Name
                    })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = string.IsNullOrWhiteSpace(g.Key.ProductName)
                            ? $"SP #{g.Key.ProductId}"
                            : g.Key.ProductName,
                        Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                TopProducts.Clear();
                TopProductNames.Clear();

                foreach (var p in topProducts)
                {
                    TopProducts.Add(p.Revenue);
                    TopProductNames.Add(p.ProductName);
                }

                // ── Revenue breakdown by category ─────────────────────────────
                RevenueFrame = orderItems
                    .Where(i => !IsServiceOrderItem(i) && IsFrameProduct(i.Product))
                    .Sum(i => i.UnitPrice * i.Quantity);

                RevenueLens = orderItems
                    .Where(i => !IsServiceOrderItem(i) && IsLensProduct(i.Product))
                    .Sum(i => i.UnitPrice * i.Quantity);

                RevenueService = orderItems
                    .Where(i => IsServiceOrderItem(i))
                    .Sum(i => i.UnitPrice * i.Quantity);

                RevenueBreakdownLabels = new List<string> { "Gọng kính", "Tròng kính", "Dịch vụ" };
                RevenueBreakdownValues = new List<decimal> { RevenueFrame, RevenueLens, RevenueService };
            }
            else
            {
                Revenue = 0m;
                RevenueToday = 0m;
                TodayVsYesterdayPct = "0";

                RevenueFrame = 0m;
                RevenueLens = 0m;
                RevenueService = 0m;
                RevenueBreakdownLabels = new List<string> { "Gọng kính", "Tròng kính", "Dịch vụ" };
                RevenueBreakdownValues = new List<decimal> { 0m, 0m, 0m };
            }

            // ── Service orders ───────────────────────────────────────────────
            var allItems = await _db.OrderItems
                .Where(i => i.SnapshotJson != null)
                .ToListAsync();

            var svcItems = allItems.Where(i =>
                i.SnapshotJson!.Contains("\"isServiceOrder\":true") ||
                i.SnapshotJson!.Contains("\"lensProductId\":")).ToList();

            SvcTotal = svcItems.Count;

            foreach (var item in svcItems)
            {
                string status = "Pending";

                try
                {
                    var doc = JsonDocument.Parse(item.SnapshotJson!);
                    if (doc.RootElement.TryGetProperty("serviceStatus", out var sv))
                        status = sv.GetString() ?? "Pending";
                }
                catch
                {
                }

                if (status == "Pending") SvcPending++;
                if (status == "Processing") SvcProcessing++;
                if (status == "Ready") SvcReady++;
            }

            // ── Appointments ────────────────────────────────────────────────
            var appts = _db.EyeExamAppointments.AsQueryable();

            ApptTotal = await appts.CountAsync();
            ApptPending = await appts.CountAsync(a => a.Status == "Pending");
            ApptConfirmed = await appts.CountAsync(a => a.Status == "Confirmed");
            ApptToday = await appts.CountAsync(a => a.AppointmentDate == DateOnly.FromDateTime(today));

            // ── Alerts ───────────────────────────────────────────────────────
            if (ApptPending > 0)
            {
                Alerts.Add(new AlertItem(
                    "warn",
                    $"{ApptPending} eye exam appointment{(ApptPending > 1 ? "s" : "")} awaiting confirmation",
                    "/Appointments"));
            }

            if (SvcPending > 0)
            {
                Alerts.Add(new AlertItem(
                    "info",
                    $"{SvcPending} service order{(SvcPending > 1 ? "s" : "")} pending — assign a technician",
                    "/Staff/ServiceOrders"));
            }

            if (SvcReady > 0)
            {
                Alerts.Add(new AlertItem(
                    "ok",
                    $"{SvcReady} service order{(SvcReady > 1 ? "s" : "")} ready for customer pickup",
                    "/Staff/ServiceOrders"));
            }

            if (OrderPending > 0)
            {
                Alerts.Add(new AlertItem(
                    "info",
                    $"{OrderPending} regular order{(OrderPending > 1 ? "s" : "")} pending fulfillment",
                    "/Admin/Orders"));
            }

            // ── Recent orders ───────────────────────────────────────────────
            RecentOrders = await _db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(6)
                .Select(o => new RecentOrder
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User != null ? o.User.FullName : o.ReceiverName,
                    Total = o.TotalAmount,
                    Status = o.Status ?? "Pending",
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            if (!IsAdmin)
            {
                foreach (var o in RecentOrders)
                    o.Total = 0m;
            }

            // ── Upcoming appointments ───────────────────────────────────────
            UpcomingAppts = await _db.EyeExamAppointments
                .Where(a => a.AppointmentDate >= DateOnly.FromDateTime(today)
                         && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .Take(5)
                .ToListAsync();

            // ── Pending service orders ───────────────────────────────────────
            var pendingRows = new List<SvcOrderRow>();

            foreach (var item in svcItems.Take(100))
            {
                string status = "Pending";
                string frame = "";
                string service = "";

                try
                {
                    var doc = JsonDocument.Parse(item.SnapshotJson!);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("serviceStatus", out var sv))
                        status = sv.GetString() ?? "Pending";

                    if (root.TryGetProperty("frameName", out var fn))
                        frame = fn.GetString() ?? "";

                    if (root.TryGetProperty("serviceName", out var sn))
                        service = sn.GetString() ?? "";
                }
                catch
                {
                }

                if (status == "Pending")
                {
                    pendingRows.Add(new SvcOrderRow
                    {
                        OrderItemId = item.OrderItemId,
                        OrderId = item.OrderId,
                        FrameName = frame,
                        ServiceName = service
                    });
                }
            }

            PendingSvcOrders = pendingRows.Take(5).ToList();
        }

        private static bool IsServiceOrderItem(OrderItem item)
        {
            if (string.IsNullOrWhiteSpace(item.SnapshotJson))
                return false;

            return item.SnapshotJson.Contains("\"isServiceOrder\":true")
                || item.SnapshotJson.Contains("\"lensProductId\":");
        }

        private static bool IsLensProduct(Product? product)
        {
            if (product == null) return false;

            return product is Lens
                || string.Equals(product.ProductType, "Lens", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product.ProductType, "Lenses", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFrameProduct(Product? product)
        {
            if (product == null) return false;

            return product is Frame
                || string.Equals(product.ProductType, "Frame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(product.ProductType, "Frames", StringComparison.OrdinalIgnoreCase);
        }

        public record AlertItem(string Type, string Message, string Link);

        public class RecentOrder
        {
            public int OrderId { get; set; }
            public string? CustomerName { get; set; }
            public decimal Total { get; set; }
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        public class SvcOrderRow
        {
            public int OrderItemId { get; set; }
            public int OrderId { get; set; }
            public string FrameName { get; set; } = "";
            public string ServiceName { get; set; } = "";
        }
    }
}