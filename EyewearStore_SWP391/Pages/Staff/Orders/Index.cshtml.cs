using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EyewearStore_SWP391.Pages.Staff.Orders
{
    /// <summary>
    /// Operations Staff Order Management
    /// </summary>
    [Authorize(Roles = "staff,operational,admin")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        // Properties
        public List<Order> Orders { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        // Available Statuses
        public List<string> Statuses { get; } = new()
        {
            "Pending Confirmation",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed",
            "Cancelled"
        };

        // Dashboard Stats Class
        public class DashboardStats
        {
            public int PendingCount { get; set; }
            public int ProcessingCount { get; set; }
            public int PrescriptionCount { get; set; }
            public int ShippedTodayCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Calculate Dashboard Stats
            await CalculateStatsAsync();

            // Build query
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Shipments)
                .AsQueryable();

            // Default filter: show orders requiring operations attention if no StatusFilter
            if (string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(o =>
                    o.Status == "Confirmed" ||
                    o.Status == "Processing" ||
                    o.Status == "Shipped");
            }
            else
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // Search Filter
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var searchTerm = Search.Trim();
                if (int.TryParse(searchTerm, out var orderId))
                {
                    query = query.Where(o =>
                        o.OrderId == orderId ||
                        (o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm))));
                }
                else
                {
                    query = query.Where(o =>
                        o.User != null && (o.User.Email.Contains(searchTerm) || o.User.FullName.Contains(searchTerm)));
                }
            }

            // Get results
            var allOrders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(500)
                .AsNoTracking()
                .ToListAsync();

            // Filter by Type (in-memory)
            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                if (TypeFilter == "Prescription")
                {
                    allOrders = allOrders
                        .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null))
                        .ToList();
                }
                else if (TypeFilter == "ReadyStock")
                {
                    allOrders = allOrders
                        .Where(o => !o.OrderItems.Any(oi => oi.PrescriptionId != null) &&
                                    o.OrderItems.All(oi => (oi.Product?.InventoryQty ?? 0) >= oi.Quantity))
                        .ToList();
                }
                else if (TypeFilter == "PreOrder")
                {
                    allOrders = allOrders
                        .Where(o => o.OrderItems.Any(oi => (oi.Product?.InventoryQty ?? 0) < oi.Quantity))
                        .ToList();
                }
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                if (PriorityFilter == "High")
                {
                    allOrders = allOrders
                        .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null) ||
                                    o.CreatedAt < DateTime.UtcNow.AddDays(-2))
                        .ToList();
                }
                else if (PriorityFilter == "Normal")
                {
                    allOrders = allOrders
                        .Where(o => !o.OrderItems.Any(oi => oi.PrescriptionId != null) &&
                                    o.CreatedAt >= DateTime.UtcNow.AddDays(-2))
                        .ToList();
                }
            }

            Orders = allOrders;
        }

        private async Task CalculateStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Pending Confirmation count
            Stats.PendingCount = await _context.Orders
                .CountAsync(o => o.Status == "Pending Confirmation");

            // Processing count (Confirmed + Processing)
            Stats.ProcessingCount = await _context.Orders
                .CountAsync(o => o.Status == "Confirmed" || o.Status == "Processing");

            // Prescription Orders count (active)
            Stats.PrescriptionCount = await _context.Orders
                .Where(o => o.Status != "Completed" && o.Status != "Cancelled")
                .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null))
                .CountAsync();

            // Shipped Today count -> count shipments with ShippedAt in today
            Stats.ShippedTodayCount = await _context.Shipments
                .Where(s => s.ShippedAt != null && s.ShippedAt >= today && s.ShippedAt < tomorrow)
                .CountAsync();
        }
    }
}