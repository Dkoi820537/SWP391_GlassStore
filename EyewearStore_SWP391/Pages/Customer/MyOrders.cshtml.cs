using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Json;
using System;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize(Roles = "customer,Customer")]
    public class MyOrdersModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public MyOrdersModel(EyewearStoreContext context) => _context = context;

        // ── DTOs ─────────────────────────────────────────────────────────────

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string? TrackingNumber { get; set; }

            public string ReceiverName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string AddressLine { get; set; } = "";

            public bool HasPrescription { get; set; }

            public List<OrderProductViewModel> Products { get; set; } = new();
        }

        public class OrderProductViewModel
        {
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }

            // Prescription
            public int? PrescriptionId { get; set; }
            public PrescriptionViewModel? Prescription { get; set; }

            // Đơn gia công
            public bool IsServiceOrder { get; set; }
            public string? LensName { get; set; }
            public decimal? LensPrice { get; set; }
            public string? ServiceName { get; set; }
            public decimal? ServicePrice { get; set; }
            public decimal? FramePrice { get; set; }
        }

        public class PrescriptionViewModel
        {
            public string ProfileName { get; set; } = "";
            public decimal? RightSph { get; set; }
            public decimal? RightCyl { get; set; }
            public int? RightAxis { get; set; }
            public decimal? LeftSph { get; set; }
            public decimal? LeftCyl { get; set; }
            public int? LeftAxis { get; set; }
        }

        public List<OrderViewModel> Orders { get; set; } = new();
        public string CurrentUserEmail { get; set; } = "";
        public string CurrentUserName { get; set; } = "";

        // ── Helper: đọc SnapshotJson cho đơn gia công ────────────────────────

        private static (bool isServiceOrder, string? lensName, decimal? lensPrice,
                        string? serviceName, decimal? servicePrice, decimal? framePrice)
            ParseSnapshot(string? snapshotJson)
        {
            if (string.IsNullOrEmpty(snapshotJson))
                return (false, null, null, null, null, null);

            try
            {
                var doc = JsonDocument.Parse(snapshotJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("isServiceOrder", out var isSvcEl)
                    || isSvcEl.ValueKind != JsonValueKind.True)
                    return (false, null, null, null, null, null);

                string? lensName = root.TryGetProperty("lensProductName", out var ln) ? ln.GetString() : null;
                decimal? lensPrice = root.TryGetProperty("lensPrice", out var lp) && lp.TryGetDecimal(out var lpv) ? lpv : null;
                string? serviceName = root.TryGetProperty("serviceName", out var sn) ? sn.GetString() : null;
                decimal? svcPrice = root.TryGetProperty("servicePrice", out var sp) && sp.TryGetDecimal(out var spv) ? spv : null;
                decimal? framePrice = root.TryGetProperty("framePrice", out var fp) && fp.TryGetDecimal(out var fpv) ? fpv : null;

                return (true, lensName, lensPrice, serviceName, svcPrice, framePrice);
            }
            catch { return (false, null, null, null, null, null); }
        }

        // ── OnGetAsync ───────────────────────────────────────────────────────

        public async Task OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    Orders = new List<OrderViewModel>();
                    return;
                }

                var user = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) { Orders = new List<OrderViewModel>(); return; }

                CurrentUserEmail = user.Email ?? "";
                CurrentUserName = user.FullName ?? user.Email ?? "";

                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Prescription)
                    .Include(o => o.Shipments)
                    .OrderByDescending(o => o.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();

                Orders = orders.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                    PaymentMethod = o.PaymentMethod ?? "Stripe",
                    TrackingNumber = o.Shipments.FirstOrDefault()?.TrackingNumber,
                    ReceiverName = o.ReceiverName ?? "",
                    Phone = o.Phone ?? "",
                    AddressLine = o.AddressLine ?? "",
                    HasPrescription = o.OrderItems.Any(oi => oi.PrescriptionId.HasValue),

                    Products = o.OrderItems.Select(oi =>
                    {
                        var (isServiceOrder, lensName, lensPrice,
                             serviceName, servicePrice, framePrice) = ParseSnapshot(oi.SnapshotJson);

                        // Tên hiển thị
                        string displayName = isServiceOrder
                            ? $"{oi.Product?.Name ?? "Frame"} + {lensName} + {serviceName}"
                            : (oi.Product?.Name ?? "Product");

                        return new OrderProductViewModel
                        {
                            Name = displayName,
                            Sku = oi.Product?.Sku ?? "N/A",
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            PrescriptionId = oi.PrescriptionId,
                            Prescription = oi.Prescription != null ? new PrescriptionViewModel
                            {
                                ProfileName = oi.Prescription.ProfileName ?? "Prescription",
                                RightSph = oi.Prescription.RightSph,
                                RightCyl = oi.Prescription.RightCyl,
                                RightAxis = oi.Prescription.RightAxis,
                                LeftSph = oi.Prescription.LeftSph,
                                LeftCyl = oi.Prescription.LeftCyl,
                                LeftAxis = oi.Prescription.LeftAxis
                            } : null,

                            // Đơn gia công
                            IsServiceOrder = isServiceOrder,
                            LensName = lensName,
                            LensPrice = lensPrice,
                            ServiceName = serviceName,
                            ServicePrice = servicePrice,
                            FramePrice = framePrice
                        };
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
                Orders = new List<OrderViewModel>();
            }
        }

        // ── Helper methods ───────────────────────────────────────────────────

        public string GetStatusBadgeClass(string status) => status switch
        {
            "Pending" => "bg-warning text-dark",
            "Confirmed" => "bg-info",
            "Processing" => "bg-primary",
            "Shipped" => "bg-secondary",
            "Delivered" => "bg-success",
            "Completed" => "bg-success",
            "Cancelled" => "bg-danger",
            "Pending Confirmation" => "bg-warning text-dark",
            _ => "bg-secondary"
        };

        public string GetStatusIcon(string status) => status switch
        {
            "Pending" => "bi-hourglass-split",
            "Pending Confirmation" => "bi-hourglass-split",
            "Confirmed" => "bi-check-circle",
            "Processing" => "bi-gear",
            "Shipped" => "bi-truck",
            "Delivered" => "bi-box-seam",
            "Completed" => "bi-check-all",
            "Cancelled" => "bi-x-circle",
            _ => "bi-question-circle"
        };

        public int GetStatusProgress(string status) => status switch
        {
            "Pending" => 10,
            "Pending Confirmation" => 14,
            "Confirmed" => 28,
            "Processing" => 42,
            "Shipped" => 71,
            "Delivered" => 85,
            "Completed" => 100,
            "Cancelled" => 0,
            _ => 0
        };
    }
}
