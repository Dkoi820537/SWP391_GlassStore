using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize(Roles = "customer,Customer")]
    public class MyOrdersModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public MyOrdersModel(EyewearStoreContext context) => _context = context;

        public class OrderItem
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string TrackingNumber { get; set; } = "";
            public string ShippingAddress { get; set; } = "";

            // ✅ NEW: Prescription indicator
            public bool HasPrescription { get; set; }

            // Product details
            public List<ProductItem> Products { get; set; } = new();
        }

        public class ProductItem
        {
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }

            // ✅ NEW: Prescription details
            public int? PrescriptionId { get; set; }
            public PrescriptionInfo? Prescription { get; set; }
        }

        // ✅ NEW: Prescription info class
        public class PrescriptionInfo
        {
            public string ProfileName { get; set; } = "";
            public decimal? RightSph { get; set; }
            public decimal? RightCyl { get; set; }
            public int? RightAxis { get; set; }
            public decimal? LeftSph { get; set; }
            public decimal? LeftCyl { get; set; }
            public int? LeftAxis { get; set; }
        }

        public List<OrderItem> Orders { get; set; } = new();
        public string CurrentUserEmail { get; set; } = "";
        public string CurrentUserName { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    Orders = new List<OrderItem>();
                    return;
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    Orders = new List<OrderItem>();
                    return;
                }

                CurrentUserEmail = user.Email ?? "";
                CurrentUserName = user.FullName ?? user.Email ?? "";

                // ✅ FIXED: Include Prescription in OrderItems!
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Prescription)  // ← KEY FIX!
                    .Include(o => o.Address)
                    .Include(o => o.Shipments)
                    .OrderByDescending(o => o.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();

                Orders = orders.Select(o => new OrderItem
                {
                    OrderId = o.OrderId,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                    PaymentMethod = o.PaymentMethod ?? "Stripe",
                    TrackingNumber = o.Shipments.FirstOrDefault()?.TrackingNumber ?? "",
                    ShippingAddress = o.ReceiverName + " — " + o.Phone + " — " + o.AddressLine,  // ✅ Use snapshot

                    // ✅ NEW: Check if order has prescription
                    HasPrescription = o.OrderItems.Any(oi => oi.PrescriptionId.HasValue),

                    Products = o.OrderItems.Select(oi => new ProductItem
                    {
                        Name = oi.Product?.Name ?? "Deleted product",
                        Sku = oi.Product?.Sku ?? "N/A",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,

                        // ✅ NEW: Include prescription details
                        PrescriptionId = oi.PrescriptionId,
                        Prescription = oi.Prescription != null ? new PrescriptionInfo
                        {
                            ProfileName = oi.Prescription.ProfileName ?? "Prescription",
                            RightSph = oi.Prescription.RightSph,
                            RightCyl = oi.Prescription.RightCyl,
                            RightAxis = oi.Prescription.RightAxis,
                            LeftSph = oi.Prescription.LeftSph,
                            LeftCyl = oi.Prescription.LeftCyl,
                            LeftAxis = oi.Prescription.LeftAxis
                        } : null
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
                Orders = new List<OrderItem>();
            }
        }

        // Helper methods (keep existing ones)
        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending Confirmation" => "bg-warning text-dark",
                "Pending" => "bg-warning text-dark",
                "Confirmed" => "bg-info",
                "Processing" => "bg-primary",
                "Shipped" => "bg-secondary",
                "Delivered" => "bg-success",
                "Completed" => "bg-success",
                "Cancelled" => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending Confirmation" => "bi-hourglass-split",
                "Pending" => "bi-hourglass-split",
                "Confirmed" => "bi-check-circle",
                "Processing" => "bi-gear",
                "Shipped" => "bi-truck",
                "Delivered" => "bi-box-seam",
                "Completed" => "bi-check-all",
                "Cancelled" => "bi-x-circle",
                _ => "bi-question-circle"
            };
        }

        public static int GetStatusProgress(string status)
        {
            return status switch
            {
                "Pending Confirmation" => 14,
                "Pending" => 14,
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
}
