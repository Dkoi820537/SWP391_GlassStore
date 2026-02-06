using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace EyewearStore_SWP391.Pages.Support.Customers
{
    [Authorize(Roles = "support,sales,sale,admin,Administrator")]
    public class OrderHistoryModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public OrderHistoryModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public List<OrderDto> Orders { get; set; } = new();
        public CustomerStats Stats { get; set; } = new();

        public class OrderDto
        {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; } = "";
            public int ItemCount { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class CustomerStats
        {
            public int TotalOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CancelledOrders { get; set; }
            public decimal TotalSpent { get; set; }
        }

        public async Task OnGetAsync(int userId)
        {
            // Load customer info
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                CustomerName = "Unknown Customer";
                CustomerEmail = "Not found";
                return;
            }

            CustomerName = user.FullName ?? user.Email ?? "Unknown";
            CustomerEmail = user.Email ?? "N/A";

            // Load all orders with OrderItems included to get count
            var orders = await _context.Orders
                .Include(o => o.OrderItems)  // IMPORTANT: Include OrderItems to count them
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Map to DTO with ItemCount
            Orders = orders.Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                ItemCount = o.OrderItems.Count,  // Count items from navigation property
                TotalAmount = o.TotalAmount
            }).ToList();

            // Calculate stats
            Stats = new CustomerStats
            {
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.Status == "Completed"),
                CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                TotalSpent = orders
                    .Where(o => o.Status != "Cancelled")
                    .Sum(o => o.TotalAmount)
            };
        }
    }
}
