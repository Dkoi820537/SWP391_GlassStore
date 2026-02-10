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

            // Product details
            public List<ProductItem> Products { get; set; } = new();
        }

        public class ProductItem
        {
            public string Name { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        public List<OrderItem> Orders { get; set; } = new();
        public string CurrentUserEmail { get; set; } = "";
        public string CurrentUserName { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                // Method 1: Get email from User.Identity.Name
                CurrentUserEmail = User.Identity?.Name ?? "";

                // Method 2: Try from Claims (backup)
                if (string.IsNullOrEmpty(CurrentUserEmail))
                {
                    CurrentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                }

                // Method 3: Try from NameIdentifier (backup)
                if (string.IsNullOrEmpty(CurrentUserEmail))
                {
                    CurrentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                }

                if (string.IsNullOrEmpty(CurrentUserEmail))
                {
                    Orders = new List<OrderItem>();
                    return;
                }

                // Find user in database
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == CurrentUserEmail);

                if (user == null)
                {
                    Orders = new List<OrderItem>();
                    return;
                }

                CurrentUserName = user.FullName ?? user.Email;

                // Get all orders for this user
                var orders = await _context.Orders
                    .Where(o => o.UserId == user.UserId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
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
                    PaymentMethod = o.PaymentMethod ?? "COD",
                    TrackingNumber = o.Shipments.FirstOrDefault()?.TrackingNumber ?? "",
                    ShippingAddress = o.Address?.AddressLine ?? "No address provided",
                    Products = o.OrderItems.Select(oi => new ProductItem
                    {
                        Name = oi.Product?.Name ?? "Deleted product",
                        Sku = oi.Product?.Sku ?? "N/A",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log error (in production use ILogger)
                Console.WriteLine($"Error loading orders: {ex.Message}");
                Orders = new List<OrderItem>();
            }
        }

        // Helper method to get badge class by status
        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending Confirmation" => "bg-warning text-dark",
                "Confirmed" => "bg-info",
                "Processing" => "bg-primary",
                "Shipped" => "bg-secondary",
                "Delivered" => "bg-success",
                "Completed" => "bg-success",
                "Cancelled" => "bg-danger",
                _ => "bg-secondary"
            };
        }

        // Helper method to get icon by status
        public static string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending Confirmation" => "bi-hourglass-split",
                "Confirmed" => "bi-check-circle",
                "Processing" => "bi-gear",
                "Shipped" => "bi-truck",
                "Delivered" => "bi-box-seam",
                "Completed" => "bi-check-all",
                "Cancelled" => "bi-x-circle",
                _ => "bi-question-circle"
            };
        }

        // Helper method to display progress
        public static int GetStatusProgress(string status)
        {
            return status switch
            {
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

        // Helper method to display status label
        public static string GetStatusVietnamese(string status)
        {
            return status switch
            {
                "Pending Confirmation" => "Pending Confirmation",
                "Confirmed" => "Confirmed",
                "Processing" => "Processing",
                "Shipped" => "Shipped",
                "Delivered" => "Delivered",
                "Completed" => "Completed",
                "Cancelled" => "Cancelled",
                _ => status
            };
        }
    }
}
