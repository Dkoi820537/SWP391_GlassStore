using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Admin.ServiceOrders
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public IndexModel(EyewearStoreContext context) => _context = context;

        // ── Filters ──────────────────────────────────────────────────────────
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public const int PageSize = 12;

        // ── Page data ─────────────────────────────────────────────────────────
        public List<ServiceOrderRow> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrev => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        // Stats
        public int StatTotal { get; set; }
        public int StatPending { get; set; }
        public int StatProcessing { get; set; }
        public int StatReady { get; set; }
        public int StatDone { get; set; }

        // ── View model ────────────────────────────────────────────────────────
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
            public decimal FramePrice { get; set; }
            public decimal LensPrice { get; set; }
            public decimal ServicePrice { get; set; }
            public decimal Total { get; set; }
            public string? AssignedTo { get; set; }
            public string? InternalNote { get; set; }
        }

        public async Task OnGetAsync()
        {
            // ── 1. Load all OrderItems that have ServiceId set ────────────────
            //    This covers BOTH old orders (no isServiceOrder flag) and new ones.
            var serviceItems = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.OrderItems.Any(x => x.SnapshotJson != null
                    && (x.SnapshotJson.Contains("\"isServiceOrder\":true")
                        || x.SnapshotJson.Contains("\"lensProductId\":"))))
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

            // ── 2. Collect all lensProductIds to load names in one query ──────
            var lensIdMap = new Dictionary<int, string>(); // productId → name
            var lensIds = new List<int>();

            foreach (var oi in serviceItems)
            {
                if (string.IsNullOrEmpty(oi.SnapshotJson)) continue;
                try
                {
                    var snap = JsonDocument.Parse(oi.SnapshotJson);
                    // New format: lensProductId in snapshot
                    if (snap.RootElement.TryGetProperty("lensProductId", out var lEl)
                        && lEl.TryGetInt32(out var lid))
                        lensIds.Add(lid);
                }
                catch { }
            }

            if (lensIds.Any())
            {
                lensIdMap = await _context.Products
                    .Where(p => lensIds.Distinct().Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId, p => p.Name);
            }

            // ── 3. Build rows ─────────────────────────────────────────────────
            var rows = new List<ServiceOrderRow>();

            foreach (var oi in serviceItems)
            {
                if (string.IsNullOrEmpty(oi.SnapshotJson)) continue;

                ServiceSnapshot snap;
                try
                {
                    snap = JsonSerializer.Deserialize<ServiceSnapshot>(oi.SnapshotJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new ServiceSnapshot();
                }
                catch { snap = new ServiceSnapshot(); }

                // Detect service order: either flag set OR has lensProductId field
                bool isServiceOrder = snap.IsServiceOrder
                    || snap.LensProductId.HasValue
                    || oi.SnapshotJson.Contains("\"lensProductId\":");

                if (!isServiceOrder) continue;

                // Resolve lens name
                string lensName = snap.LensProductName ?? "";
                if (string.IsNullOrEmpty(lensName) && snap.LensProductId.HasValue
                    && lensIdMap.TryGetValue(snap.LensProductId.Value, out var ln))
                    lensName = ln;

                rows.Add(new ServiceOrderRow
                {
                    OrderId = oi.OrderId,
                    OrderItemId = oi.OrderItemId,
                    CustomerName = oi.Order?.User?.FullName
                                  ?? oi.Order?.ReceiverName ?? "—",
                    CustomerEmail = oi.Order?.User?.Email ?? "—",
                    CreatedAt = oi.Order?.CreatedAt ?? DateTime.MinValue,
                    Status = snap.ServiceStatus ?? "Pending",
                    FrameName = snap.FrameName ?? snap.ProductName ?? oi.Product?.Name ?? "—",
                    LensName = lensName,
                    ServiceName = snap.ServiceName ?? "—",
                    FramePrice = snap.FramePrice ?? 0m,
                    LensPrice = snap.LensPrice ?? 0m,
                    ServicePrice = snap.ServicePrice ?? 0m,
                    Total = oi.UnitPrice * oi.Quantity,
                    AssignedTo = snap.AssignedTo,
                    InternalNote = snap.InternalNote
                });
            }

            // ── 4. Stats (before filter) ──────────────────────────────────────
            StatTotal = rows.Count;
            StatPending = rows.Count(r => r.Status == "Pending");
            StatProcessing = rows.Count(r => r.Status == "Processing");
            StatReady = rows.Count(r => r.Status == "Ready");
            StatDone = rows.Count(r => r.Status == "Done");

            // ── 5. Filter ─────────────────────────────────────────────────────
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

            // ── 6. Paginate ───────────────────────────────────────────────────
            TotalCount = rows.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, TotalPages));

            Items = rows
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        // ── Snapshot DTO ──────────────────────────────────────────────────────
        // Covers BOTH old format (no isServiceOrder) and new format
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
            public int? ServiceId { get; set; }
            // Admin-managed fields
            public string? ServiceStatus { get; set; }
            public string? AssignedTo { get; set; }
            public string? InternalNote { get; set; }
        }
    }
}