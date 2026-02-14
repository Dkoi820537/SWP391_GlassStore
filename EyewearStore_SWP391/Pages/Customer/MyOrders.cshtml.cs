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

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string? TrackingNumber { get; set; }

            // ✅ Address snapshot
            public string ReceiverName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string AddressLine { get; set; } = "";

            // ✅ Prescription indicator
            public bool HasPrescription { get; set; }

            public List<OrderProductViewModel> Products { get; set; } = new();
        }

        public class OrderProductViewModel
        {
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }

            // ✅ Prescription details
            public int? PrescriptionId { get; set; }
            public PrescriptionViewModel? Prescription { get; set; }
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

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    Orders = new List<OrderViewModel>();
                    return;
                }

                CurrentUserEmail = user.Email ?? "";
                CurrentUserName = user.FullName ?? user.Email ?? "";

                // ✅ Load orders with prescription details
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Prescription)  // ← KEY: Load prescription!
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

                    // ✅ Use address snapshot
                    ReceiverName = o.ReceiverName,
                    Phone = o.Phone,
                    AddressLine = o.AddressLine,

                    // ✅ Check if order has prescription
                    HasPrescription = o.OrderItems.Any(oi => oi.PrescriptionId.HasValue),

                    Products = o.OrderItems.Select(oi => new OrderProductViewModel
                    {
                        Name = oi.Product?.Name ?? "Product",
                        Sku = oi.Product?.Sku ?? "N/A",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,

                        // ✅ Include prescription details
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
                        } : null
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading orders: {ex.Message}");
                Orders = new List<OrderViewModel>();
            }
        }

        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
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
