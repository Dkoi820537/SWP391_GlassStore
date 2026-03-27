using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Support.ServiceOrders
{
    [Authorize(Roles = "sale,admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _db;
        public IndexModel(EyewearStoreContext db) => _db = db;

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public const int PageSize = 15;

        public List<ServiceOrderRow> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrev => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public int StatTotal, StatPending, StatProcessing, StatReady, StatDone;

        public class ServiceOrderRow
        {
            public int OrderId { get; set; }
            public int OrderItemId { get; set; }
            public string CustomerName { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "Pending";
            public string FrameName { get; set; } = "";
            public string LensName { get; set; } = "";
            public string ServiceName { get; set; } = "";
            public decimal Total { get; set; }
            public string? AssignedTo { get; set; }
        }

        public class ServiceSnapshot
        {
            public bool IsServiceOrder { get; set; }
            public string? ProductName { get; set; }
            public string? FrameName { get; set; }
            public string? LensProductName { get; set; }
            public int? LensProductId { get; set; }
            public string? ServiceName { get; set; }
            public decimal? FramePrice { get; set; }
            public decimal? LensPrice { get; set; }
            public decimal? ServicePrice { get; set; }
            public string? ServiceStatus { get; set; }
            public string? AssignedTo { get; set; }
            public string? InternalNote { get; set; }
        }

        public async Task OnGetAsync()
        {
            var serviceItems = await _db.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.SnapshotJson != null &&
                    (oi.SnapshotJson.Contains("\"isServiceOrder\":true") ||
                     oi.SnapshotJson.Contains("\"lensProductId\":")))
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

            var rows = new List<ServiceOrderRow>();
            foreach (var oi in serviceItems)
            {
                if (string.IsNullOrEmpty(oi.SnapshotJson)) continue;
                ServiceSnapshot snap;
                try
                {
                    snap = JsonSerializer.Deserialize<ServiceSnapshot>(
                        oi.SnapshotJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch { snap = new(); }

                rows.Add(new ServiceOrderRow
                {
                    OrderId = oi.OrderId,
                    OrderItemId = oi.OrderItemId,
                    CustomerName = oi.Order?.User?.FullName ?? oi.Order?.ReceiverName ?? "—",
                    CustomerEmail = oi.Order?.User?.Email ?? "—",
                    CreatedAt = oi.Order?.CreatedAt ?? DateTime.MinValue,
                    Status = snap.ServiceStatus ?? "Pending",
                    FrameName = snap.FrameName ?? snap.ProductName ?? oi.Product?.Name ?? "—",
                    LensName = snap.LensProductName ?? "",
                    ServiceName = snap.ServiceName ?? "—",
                    Total = oi.UnitPrice * oi.Quantity,
                    AssignedTo = snap.AssignedTo
                });
            }

            StatTotal = rows.Count;
            StatPending = rows.Count(r => r.Status == "Pending");
            StatProcessing = rows.Count(r => r.Status == "Processing");
            StatReady = rows.Count(r => r.Status == "Ready");
            StatDone = rows.Count(r => r.Status == "Done");

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var s = SearchTerm.Trim().ToLower();
                rows = rows.Where(r =>
                    r.CustomerName.ToLower().Contains(s) ||
                    r.CustomerEmail.ToLower().Contains(s) ||
                    r.OrderId.ToString().Contains(s) ||
                    r.FrameName.ToLower().Contains(s) ||
                    r.ServiceName.ToLower().Contains(s)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
                rows = rows.Where(r => r.Status == StatusFilter).ToList();

            TotalCount = rows.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));
            Items = rows.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }
}
