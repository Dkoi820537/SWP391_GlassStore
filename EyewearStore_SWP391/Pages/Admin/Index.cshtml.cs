using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _db;
        public IndexModel(EyewearStoreContext db) => _db = db;

        /// <summary>
        /// True when the logged-in user has the "admin" role.
        /// Used to gate financial data on both backend and frontend.
        /// </summary>
        public bool IsAdmin => User.IsInRole("admin");

        // ── Order stats ───────────────────────────────────────────────────────
        public int OrderTotal { get; set; }
        public int OrderPending { get; set; }
        public int OrderToday { get; set; }
        public decimal Revenue { get; set; }
        public decimal RevenueToday { get; set; }

        // ── Service order stats ───────────────────────────────────────────────
        public int SvcTotal { get; set; }
        public int SvcPending { get; set; }
        public int SvcProcessing { get; set; }
        public int SvcReady { get; set; }

        // ── Appointment stats ─────────────────────────────────────────────────
        public int ApptTotal { get; set; }
        public int ApptPending { get; set; }
        public int ApptToday { get; set; }
        public int ApptConfirmed { get; set; }

        // ── Alerts (things needing attention) ─────────────────────────────────
        public List<AlertItem> Alerts { get; set; } = new();

        // ── Recent ───────────────────────────────────────────────────────────
        public List<RecentOrder> RecentOrders { get; set; } = new();
        public List<EyeExamAppointment> UpcomingAppts { get; set; } = new();
        public List<SvcOrderRow> PendingSvcOrders { get; set; } = new();

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;

            // ── Orders ───────────────────────────────────────────────────────
            var orders = _db.Orders.AsQueryable();
            OrderTotal = await orders.CountAsync();
            OrderPending = await orders.CountAsync(o => o.Status == "Pending" || o.Status == "Processing");
            OrderToday = await orders.CountAsync(o => o.CreatedAt.Date == today);
            // ── Financial data: Admin-only (defense-in-depth) ──────────
            if (IsAdmin)
            {
                Revenue = await orders.Where(o => o.Status != "Cancelled")
                                           .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                RevenueToday = await orders.Where(o => o.CreatedAt.Date == today && o.Status != "Cancelled")
                                           .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            }

            // ── Service orders (scan SnapshotJson) ───────────────────────────
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
                catch { }
                if (status == "Pending") SvcPending++;
                if (status == "Processing") SvcProcessing++;
                if (status == "Ready") SvcReady++;
            }

            // ── Appointments ─────────────────────────────────────────────────
            var appts = _db.EyeExamAppointments.AsQueryable();
            ApptTotal = await appts.CountAsync();
            ApptPending = await appts.CountAsync(a => a.Status == "Pending");
            ApptConfirmed = await appts.CountAsync(a => a.Status == "Confirmed");
            ApptToday = await appts.CountAsync(a =>
                a.AppointmentDate == DateOnly.FromDateTime(today));

            // ── Alerts ───────────────────────────────────────────────────────
            if (ApptPending > 0)
                Alerts.Add(new AlertItem("warn",
                    $"{ApptPending} eye exam appointment{(ApptPending > 1 ? "s" : "")} awaiting confirmation",
                    "/Admin/Appointments"));

            if (SvcPending > 0)
                Alerts.Add(new AlertItem("info",
                    $"{SvcPending} service order{(SvcPending > 1 ? "s" : "")} pending — assign a technician",
                    "/Admin/ServiceOrders"));

            if (SvcReady > 0)
                Alerts.Add(new AlertItem("ok",
                    $"{SvcReady} service order{(SvcReady > 1 ? "s" : "")} ready for customer pickup",
                    "/Admin/ServiceOrders"));

            if (OrderPending > 0)
                Alerts.Add(new AlertItem("info",
                    $"{OrderPending} regular order{(OrderPending > 1 ? "s" : "")} pending fulfillment",
                    "/Admin/Orders"));

            // ── Recent orders (last 6) ────────────────────────────────────────
            RecentOrders = await _db.Orders
                .Include(o => o.User)
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

            // ── Strip financial data for non-admin (Manager) ──────────────────
            if (!IsAdmin)
            {
                foreach (var o in RecentOrders)
                    o.Total = 0;
            }

            // ── Upcoming appointments (next 5) ────────────────────────────────
            UpcomingAppts = await _db.EyeExamAppointments
                .Where(a => a.AppointmentDate >= DateOnly.FromDateTime(today)
                         && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.TimeSlot)
                .Take(5)
                .ToListAsync();

            // ── Pending service orders ────────────────────────────────────────
            var pendingItemIds = new List<SvcOrderRow>();
            foreach (var item in svcItems.Take(100))
            {
                string status = "Pending"; string frame = ""; string service = "";
                try
                {
                    var doc = JsonDocument.Parse(item.SnapshotJson!);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("serviceStatus", out var sv)) status = sv.GetString() ?? "Pending";
                    if (root.TryGetProperty("frameName",    out var fn)) frame   = fn.GetString() ?? "";
                    if (root.TryGetProperty("serviceName",  out var sn)) service = sn.GetString() ?? "";
                }
                catch { }
                if (status == "Pending")
                    pendingItemIds.Add(new SvcOrderRow { OrderItemId = item.OrderItemId, OrderId = item.OrderId, FrameName = frame, ServiceName = service });
            }
            PendingSvcOrders = pendingItemIds.Take(5).ToList();
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
