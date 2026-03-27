using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Admin.Orders
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public IndexModel(EyewearStoreContext context) => _context = context;

        // ----- Stats -----
        public int StatTotal        { get; set; }
        public int StatPending      { get; set; }   // Sales phase
        public int StatConfirmed    { get; set; }   // Sales phase
        public int StatProcessing   { get; set; }   // Ops phase
        public int StatShipped      { get; set; }   // Ops phase
        public int StatDelivered    { get; set; }
        public int StatCompleted    { get; set; }
        public int StatCancelled    { get; set; }

        // ----- Filters -----
        [BindProperty(SupportsGet = true)] public string? Search          { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter    { get; set; }
        [BindProperty(SupportsGet = true)] public string? PhaseFilter     { get; set; }
        [BindProperty(SupportsGet = true)] public int     PageNumber      { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int     PageSize        { get; set; } = 15;

        // ----- Results -----
        public List<OrderRow> Orders { get; set; } = new();
        public int TotalOrders   { get; set; }
        public int TotalPages    { get; set; } = 1;
        public int FirstIdx      => TotalOrders == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
        public int LastIdx       => Math.Min(PageNumber * PageSize, TotalOrders);
        public List<int> PageNums { get; set; } = new();

        public static readonly string[] SalesStatuses = { "Pending", "Confirmed" };
        public static readonly string[] OpsStatuses   =
        {
            "Processing", "Processing - Lens Ordered", "Processing - Lens Received",
            "Processing - Fitting", "Processing - QC", "Processing - Packed",
            "Shipped", "Delivered", "Completed"
        };

        public class OrderRow
        {
            public int      OrderId        { get; set; }
            public string?  CustomerName   { get; set; }
            public string?  CustomerEmail  { get; set; }
            public DateTime CreatedAt      { get; set; }
            public string   Status         { get; set; } = "";
            public string   Phase          { get; set; } = "";   // "Sales" | "Ops" | "Done" | "Cancelled"
            public decimal  TotalAmount    { get; set; }
            public bool     HasPrescription { get; set; }
            public string?  TrackingNumber { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (PageSize is <= 0 or > 100) PageSize = 15;

            // ----- compute stats -----
            StatTotal      = await _context.Orders.CountAsync();
            StatPending    = await _context.Orders.CountAsync(o => o.Status == "Pending");
            StatConfirmed  = await _context.Orders.CountAsync(o => o.Status == "Confirmed");
            StatProcessing = await _context.Orders.CountAsync(o => o.Status != null && o.Status.StartsWith("Processing"));
            StatShipped    = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            StatDelivered  = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            StatCompleted  = await _context.Orders.CountAsync(o => o.Status == "Completed");
            StatCancelled  = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            // ----- build query -----
            var q = _context.Orders
                .AsNoTracking()
                .Select(o => new OrderRow
                {
                    OrderId         = o.OrderId,
                    CustomerName    = o.User != null ? o.User.FullName : o.ReceiverName,
                    CustomerEmail   = o.User != null ? o.User.Email : null,
                    CreatedAt       = o.CreatedAt,
                    Status          = o.Status,
                    Phase           = o.Status == "Pending" || o.Status == "Confirmed"
                                        ? "Sales"
                                        : o.Status == "Cancelled"
                                            ? "Cancelled"
                                            : o.Status == "Completed"
                                                ? "Done"
                                                : "Ops",
                    TotalAmount     = o.TotalAmount,
                    HasPrescription = o.OrderItems.Any(i => i.PrescriptionId != null),
                    TrackingNumber  = o.Shipments.Any()
                                        ? o.Shipments.OrderByDescending(s => s.ShipmentId)
                                               .Select(s => s.TrackingNumber).FirstOrDefault()
                                        : null,
                })
                .AsQueryable();

            // phase filter
            if (PhaseFilter == "Sales")
                q = q.Where(o => o.Phase == "Sales");
            else if (PhaseFilter == "Ops")
                q = q.Where(o => o.Phase == "Ops");

            // status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                if (StatusFilter == "Processing")
                    q = q.Where(o => o.Status != null && o.Status.StartsWith("Processing"));
                else
                    q = q.Where(o => o.Status == StatusFilter);
            }

            // search
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                if (int.TryParse(term, out var oid))
                    q = q.Where(o => o.OrderId == oid
                                  || (o.CustomerName  != null && o.CustomerName.Contains(term))
                                  || (o.CustomerEmail != null && o.CustomerEmail.Contains(term)));
                else
                    q = q.Where(o => (o.CustomerName  != null && o.CustomerName.Contains(term))
                                  || (o.CustomerEmail != null && o.CustomerEmail.Contains(term)));
            }

            TotalOrders = await q.CountAsync();
            TotalPages  = Math.Max(1, (int)Math.Ceiling(TotalOrders / (double)PageSize));
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Orders = await q
                .OrderByDescending(o => o.CreatedAt)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            BuildPageNums();
        }

        private void BuildPageNums()
        {
            const int win = 7;
            PageNums = new List<int>();
            if (TotalPages <= win)
            {
                for (int i = 1; i <= TotalPages; i++) PageNums.Add(i);
                return;
            }
            int left  = Math.Max(1, PageNumber - win / 2);
            int right = Math.Min(TotalPages, left + win - 1);
            if (right - left + 1 < win) left = Math.Max(1, right - win + 1);
            for (int i = left; i <= right; i++) PageNums.Add(i);
        }
    }
}
