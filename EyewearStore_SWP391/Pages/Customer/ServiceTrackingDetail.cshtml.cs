using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize]
    public class ServiceTrackingDetailModel : PageModel
    {
        private readonly EyewearStoreContext _db;

        public ServiceTrackingDetailModel(EyewearStoreContext db)
        {
            _db = db;
        }

        public TrackingRow? Item { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId, int itemId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Account/Login");

            var order = await _db.Orders
                .Where(o => o.OrderId == orderId && o.UserId == userId)
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistories)
                .Include(o => o.Address)
                .Include(o => o.Shipments)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Customer/ServiceTracking");
            }

            var item = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemId);
            if (item == null || string.IsNullOrEmpty(item.SnapshotJson))
            {
                TempData["ErrorMessage"] = "Service item not found.";
                return RedirectToPage("/Customer/ServiceTracking");
            }

            ServiceSnapshot? snap = null;
            try
            {
                snap = JsonSerializer.Deserialize<ServiceSnapshot>(
                    item.SnapshotJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { }

            if (snap == null)
            {
                TempData["ErrorMessage"] = "Could not load service details.";
                return RedirectToPage("/Customer/ServiceTracking");
            }

            // Parse workshop timeline — hỗ trợ cả string JSON lẫn array
            var svcTimeline = new List<TimelineEntry>();
            try
            {
                var snapDoc = JsonDocument.Parse(item.SnapshotJson);
                var snapRoot = snapDoc.RootElement;

                if (snapRoot.TryGetProperty("serviceTimeline", out var tlProp))
                {
                    string tlJson = tlProp.ValueKind == JsonValueKind.String
                        ? (tlProp.GetString() ?? "[]")
                        : tlProp.GetRawText();

                    var tlDoc = JsonDocument.Parse(tlJson);
                    foreach (var entry in tlDoc.RootElement.EnumerateArray())
                    {
                        svcTimeline.Add(new TimelineEntry
                        {
                            Status = entry.TryGetProperty("status", out var sv) ? sv.GetString() ?? "" : "",
                            AssignedTo = entry.TryGetProperty("assignedTo", out var av) ? av.GetString() ?? "" : "",
                            Note = entry.TryGetProperty("note", out var nv) ? nv.GetString() ?? "" : "",
                            Timestamp = entry.TryGetProperty("timestamp", out var tv)
                                         && DateTime.TryParse(tv.GetString(), out var dt) ? dt : DateTime.UtcNow,
                        });
                    }
                }
            }
            catch { }

            Item = new TrackingRow
            {
                OrderId = order.OrderId,
                OrderItemId = item.OrderItemId,
                CreatedAt = order.CreatedAt,
                OrderStatus = order.Status ?? "Pending",
                FrameName = snap.FrameName ?? snap.ProductName ?? "N/A",
                LensName = snap.LensProductName ?? "N/A",
                ServiceName = snap.ServiceName ?? "N/A",
                FramePrice = snap.FramePrice,
                LensPrice = snap.LensPrice,
                ServicePrice = snap.ServicePrice,
                ImageUrl = snap.ImageUrl,
                ServiceStatus = snap.ServiceStatus ?? "Pending",
                AssignedTo = snap.AssignedTo,
                InternalNote = snap.InternalNote,
                ServiceTimeline = svcTimeline,
                OrderStatusHistories = order.StatusHistories?
                    .OrderByDescending(h => h.CreatedAt).ToList() ?? new(),
                Address = order.Address,
                Shipment = order.Shipments?.OrderByDescending(s => s.ShipmentId).FirstOrDefault(),
                PaymentMethod = order.PaymentMethod,
                DepositAmount = order.DepositAmount,
                TotalAmount = order.TotalAmount,
                RefundAmount = order.RefundAmount,
                PaymentStatus = order.PaymentStatus,
                CancelledAt = order.CancelledAt,
            };

            return Page();
        }

        public class TrackingRow
        {
            public int OrderId { get; set; }
            public int OrderItemId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string OrderStatus { get; set; } = "";
            public string FrameName { get; set; } = "";
            public string LensName { get; set; } = "";
            public string ServiceName { get; set; } = "";
            public decimal FramePrice { get; set; }
            public decimal LensPrice { get; set; }
            public decimal ServicePrice { get; set; }
            public string? ImageUrl { get; set; }
            public string ServiceStatus { get; set; } = "";
            public string? AssignedTo { get; set; }
            public string? InternalNote { get; set; }
            public decimal Total => FramePrice + LensPrice + ServicePrice;

            public List<TimelineEntry> ServiceTimeline { get; set; } = new();
            public List<OrderStatusHistory> OrderStatusHistories { get; set; } = new();
            public Address? Address { get; set; }
            public Shipment? Shipment { get; set; }
            public string? PaymentMethod { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal? RefundAmount { get; set; }
            public string? PaymentStatus { get; set; }
            public DateTime? CancelledAt { get; set; }
        }

        public class TimelineEntry
        {
            public string Status { get; set; } = "";
            public string AssignedTo { get; set; } = "";
            public string Note { get; set; } = "";
            public DateTime Timestamp { get; set; }
        }

        private class ServiceSnapshot
        {
            public string? ProductName { get; set; }
            public string? FrameName { get; set; }
            public string? LensProductName { get; set; }
            public string? ServiceName { get; set; }
            public decimal FramePrice { get; set; }
            public decimal LensPrice { get; set; }
            public decimal ServicePrice { get; set; }
            public string? ImageUrl { get; set; }
            public string? ServiceStatus { get; set; }
            public string? AssignedTo { get; set; }
            public string? InternalNote { get; set; }
        }
    }
}