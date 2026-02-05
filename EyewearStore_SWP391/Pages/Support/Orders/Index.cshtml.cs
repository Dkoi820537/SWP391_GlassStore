using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;

namespace EyewearStore_SWP391.Pages.Support.Orders
{
    /// <summary>
    /// Support Staff Order Management - Main Dashboard
    /// Handles order verification, prescription checking, and customer support
    /// </summary>
    [Authorize(Roles = "support,sales,admin")]
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
            public int PrescriptionCount { get; set; }
            public int ReturnCount { get; set; }
            public int TodayConfirmedCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Calculate Dashboard Stats
            await CalculateStatsAsync();

            // Build query for orders requiring support attention
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prescription)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Returns)
                .Include(o => o.Shipments)
                .AsQueryable();

            // Default filter: Show orders needing support attention
            if (string.IsNullOrWhiteSpace(StatusFilter))
            {
                // Support staff focuses on: Pending Confirmation & orders with issues
                query = query.Where(o =>
                    o.Status == "Pending Confirmation" ||
                    o.OrderItems.Any(oi => oi.Returns.Any(r => r.Status == "Pending")));
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
                        (o.User != null && (
                            o.User.Email.Contains(searchTerm) ||
                            o.User.FullName.Contains(searchTerm)
                        ))
                    );
                }
                else
                {
                    query = query.Where(o =>
                        o.User != null && (
                            o.User.Email.Contains(searchTerm) ||
                            o.User.FullName.Contains(searchTerm)
                        )
                    );
                }
            }

            // Get results
            var allOrders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(500)
                .AsNoTracking()
                .ToListAsync();

            // Filter by Type (in-memory due to complex logic)
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
                        .Where(o =>
                            !o.OrderItems.Any(oi => oi.PrescriptionId != null) &&
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
                    // High priority: Prescription orders or orders older than 2 days
                    allOrders = allOrders
                        .Where(o =>
                            o.OrderItems.Any(oi => oi.PrescriptionId != null) ||
                            o.CreatedAt < DateTime.UtcNow.AddDays(-2) ||
                            o.OrderItems.Any(oi => oi.Returns.Any()))
                        .ToList();
                }
                else if (PriorityFilter == "Normal")
                {
                    allOrders = allOrders
                        .Where(o =>
                            !o.OrderItems.Any(oi => oi.PrescriptionId != null) &&
                            o.CreatedAt >= DateTime.UtcNow.AddDays(-2) &&
                            !o.OrderItems.Any(oi => oi.Returns.Any()))
                        .ToList();
                }
            }

            Orders = allOrders;
        }

        private async Task CalculateStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Pending Confirmation count (BR: Orders awaiting support verification)
            Stats.PendingCount = await _context.Orders
                .CountAsync(o => o.Status == "Pending Confirmation");

            // Prescription Orders needing verification (BR: Support must verify prescription data)
            Stats.PrescriptionCount = await _context.Orders
                .Where(o => o.Status == "Pending Confirmation" || o.Status == "Confirmed")
                .Where(o => o.OrderItems.Any(oi => oi.PrescriptionId != null))
                .CountAsync();

            // Returns/Refunds needing processing (BR: Support handles returns)
            Stats.ReturnCount = await _context.Returns
                .Where(r => r.Status == "Pending" || r.Status == "Under Review")
                .CountAsync();

            // Today's confirmed orders (Performance metric)
            Stats.TodayConfirmedCount = await _context.Orders
                .Where(o => o.Status == "Confirmed" || o.Status == "Processing" || o.Status == "Shipped")
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .CountAsync();
        }

        /// <summary>
        /// Quick confirm order without prescription verification
        /// BR: Support staff can quickly confirm simple orders
        /// </summary>
        public async Task<IActionResult> OnPostQuickConfirmAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Prescription)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // BR: Cannot quick-confirm prescription orders
            if (order.OrderItems.Any(oi => oi.PrescriptionId != null))
            {
                TempData["Error"] = "Prescription orders must be verified through the detailed process.";
                return RedirectToPage("./Details", new { id = orderId });
            }

            // BR: Verify status is pending
            if (order.Status != "Pending Confirmation")
            {
                TempData["Error"] = "Only pending orders can be confirmed.";
                return RedirectToPage();
            }

            // Update order status
            order.Status = "Confirmed";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // BR: Log activity (implement as needed)
            // await _auditService.LogActivity("Order Confirmed", orderId, User.Identity.Name);

            // BR: Send notification to customer
            // await _notificationService.SendOrderConfirmed(order);

            // BR: Notify operations staff
            // await _notificationService.NotifyOperations(order);

            TempData["Success"] = $"Order #{orderId} confirmed successfully and sent to Operations team.";
            return RedirectToPage();
        }
    }
}
