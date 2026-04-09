using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyewearStore_SWP391.Pages.Staff.ServiceOrders
{
    [Authorize(Roles = "operational,admin,manager")]
    public class DetailModel : PageModel
    {
        private readonly EyewearStoreContext _db;
        private readonly IEmailService _email;

        public DetailModel(EyewearStoreContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        // Chỉ cho phép advance theo flow, không cho phép chọn tự do
        public static readonly string[] StatusFlow = { "Pending", "Processing", "Ready", "Done" };

        [BindProperty] public int OrderItemId { get; set; }
        [BindProperty] public string? AssignedTo { get; set; }
        [BindProperty] public string? InternalNote { get; set; }

        public Order Order { get; set; } = null!;
        public OrderItem Item { get; set; } = null!;
        public User Customer { get; set; } = null!;
        public ServiceSnapshot Snap { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var result = await LoadAsync(orderId);
            if (result != null) return result;

            OrderItemId = Item.OrderItemId;
            AssignedTo = Snap.AssignedTo;
            InternalNote = Snap.InternalNote;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync(int orderId)
        {
            var result = await LoadAsync(orderId);
            if (result != null) return result;

            // Bắt buộc nhập tên kỹ thuật viên
            if (string.IsNullOrWhiteSpace(AssignedTo))
            {
                TempData["Error"] = "Vui lòng nhập tên kỹ thuật viên trước khi cập nhật trạng thái.";
                return RedirectToPage("Detail", new { orderId });
            }

            var currentStatus = Snap.ServiceStatus ?? "Pending";
            var nextStatus = GetNextStatus(currentStatus);

            if (string.IsNullOrEmpty(nextStatus))
            {
                TempData["Error"] = $"Không thể cập nhật tiếp vì trạng thái hiện tại là '{currentStatus}'.";
                return RedirectToPage("Detail", new { orderId });
            }

            // Parse snapshot hiện tại
            var raw = Item.SnapshotJson ?? "{}";
            using var doc = JsonDocument.Parse(raw);
            var mutable = new Dictionary<string, object?>();
            foreach (var kv in doc.RootElement.EnumerateObject())
                mutable[kv.Name] = JsonElementToObject(kv.Value);

            // Lấy timeline hiện tại hoặc tạo mới
            var timeline = new List<Dictionary<string, object?>>();
            if (mutable.TryGetValue("serviceTimeline", out var tlRaw) && tlRaw is string tlStr)
            {
                try { timeline = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(tlStr) ?? new(); }
                catch { timeline = new(); }
            }

            // Thêm milestone mới
            timeline.Add(new Dictionary<string, object?>
            {
                ["status"] = nextStatus,
                ["assignedTo"] = AssignedTo?.Trim(),
                ["note"] = InternalNote,
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            });

            mutable["serviceStatus"] = nextStatus;
            mutable["assignedTo"] = AssignedTo?.Trim();
            mutable["internalNote"] = InternalNote;
            mutable["serviceTimeline"] = JsonSerializer.Serialize(timeline);

            Item.SnapshotJson = JsonSerializer.Serialize(mutable);
            await _db.SaveChangesAsync();

            // Gửi email thông báo khách hàng
            if (!string.IsNullOrEmpty(Customer.Email))
            {
                try
                {
                    await _email.SendServiceOrderStatusAsync(
                        toEmail: Customer.Email,
                        customerName: Customer.FullName ?? "Customer",
                        orderId: Order.OrderId,
                        frameName: Snap.FrameName ?? Snap.ProductName ?? "N/A",
                        serviceName: Snap.ServiceName ?? "N/A",
                        newStatus: nextStatus,
                        assignedTo: AssignedTo,
                        note: InternalNote);
                }
                catch { /* non-fatal */ }
            }

            TempData["Success"] = $"Đã cập nhật trạng thái service lên: {nextStatus}";
            return RedirectToPage("Detail", new { orderId });
        }

        // Handler Cancel riêng
        public async Task<IActionResult> OnPostCancelAsync(int orderId)
        {
            var result = await LoadAsync(orderId);
            if (result != null) return result;

            var currentStatus = Snap.ServiceStatus ?? "Pending";
            if (currentStatus == "Done" || currentStatus == "Cancelled")
            {
                TempData["Error"] = "Không thể huỷ đơn hàng ở trạng thái này.";
                return RedirectToPage("Detail", new { orderId });
            }

            var raw = Item.SnapshotJson ?? "{}";
            using var doc = JsonDocument.Parse(raw);
            var mutable = new Dictionary<string, object?>();
            foreach (var kv in doc.RootElement.EnumerateObject())
                mutable[kv.Name] = JsonElementToObject(kv.Value);

            var timeline = new List<Dictionary<string, object?>>();
            if (mutable.TryGetValue("serviceTimeline", out var tlRaw) && tlRaw is string tlStr)
            {
                try { timeline = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(tlStr) ?? new(); }
                catch { timeline = new(); }
            }

            timeline.Add(new Dictionary<string, object?>
            {
                ["status"] = "Cancelled",
                ["assignedTo"] = Snap.AssignedTo,
                ["note"] = "Đơn bị huỷ bởi nhân viên.",
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            });

            mutable["serviceStatus"] = "Cancelled";
            mutable["serviceTimeline"] = JsonSerializer.Serialize(timeline);
            Item.SnapshotJson = JsonSerializer.Serialize(mutable);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(Customer.Email))
            {
                try
                {
                    await _email.SendServiceOrderStatusAsync(
                        toEmail: Customer.Email,
                        customerName: Customer.FullName ?? "Customer",
                        orderId: Order.OrderId,
                        frameName: Snap.FrameName ?? Snap.ProductName ?? "N/A",
                        serviceName: Snap.ServiceName ?? "N/A",
                        newStatus: "Cancelled",
                        assignedTo: Snap.AssignedTo,
                        note: "Đơn dịch vụ đã bị huỷ.");
                }
                catch { }
            }

            TempData["Success"] = "Đã huỷ service order thành công.";
            return RedirectToPage("Detail", new { orderId });
        }

        private static string? GetNextStatus(string current)
        {
            var idx = Array.IndexOf(StatusFlow, current);
            if (idx < 0 || idx >= StatusFlow.Length - 1) return null;
            return StatusFlow[idx + 1];
        }

        private static object? JsonElementToObject(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => el.GetRawText()
        };

        private async Task<IActionResult?> LoadAsync(int orderId)
        {
            Order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (Order == null) return NotFound();

            Customer = Order.User!;

            Item = Order.OrderItems.FirstOrDefault(i =>
                i.SnapshotJson != null && (
                    i.SnapshotJson.Contains("\"isServiceOrder\":true") ||
                    i.SnapshotJson.Contains("\"lensProductId\":")))!;

            if (Item == null) return NotFound();

            Snap = JsonSerializer.Deserialize<ServiceSnapshot>(
                Item.SnapshotJson!,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new ServiceSnapshot();

            return null;
        }
    }

    public class ServiceSnapshot
    {
        public bool IsServiceOrder { get; set; }
        public string? ProductName { get; set; }
        public string? FrameName { get; set; }
        public string? LensProductName { get; set; }
        public int? LensProductId { get; set; }
        public string? ServiceName { get; set; }
        public decimal FramePrice { get; set; }
        public decimal LensPrice { get; set; }
        public decimal ServicePrice { get; set; }
        public int? ServiceId { get; set; }
        public string? ServiceStatus { get; set; }
        public string? AssignedTo { get; set; }
        public string? InternalNote { get; set; }
        public string? ServiceTimeline { get; set; }
    }
}
