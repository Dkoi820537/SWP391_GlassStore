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
    public class ServiceTrackingModel : PageModel
    {
        private readonly EyewearStoreContext _db;

        public ServiceTrackingModel(EyewearStoreContext db)
        {
            _db = db;
        }

        public List<TrackingRow> Orders { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return;

            // Get all orders for this user that contain a service item
            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            foreach (var order in orders)
            {
                foreach (var item in order.OrderItems)
                {
                    if (string.IsNullOrEmpty(item.SnapshotJson)) continue;
                    if (!item.SnapshotJson.Contains("\"isServiceOrder\":true") &&
                        !item.SnapshotJson.Contains("\"lensProductId\":")) continue;

                    ServiceSnapshot? snap = null;
                    try
                    {
                        snap = JsonSerializer.Deserialize<ServiceSnapshot>(
                            item.SnapshotJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { continue; }
                    if (snap == null) continue;

                    Orders.Add(new TrackingRow
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
                        InternalNote = snap.InternalNote
                    });
                }
            }
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
        }

        // Reuse same DTO
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