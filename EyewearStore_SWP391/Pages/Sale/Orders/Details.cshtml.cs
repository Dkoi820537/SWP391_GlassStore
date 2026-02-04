using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class DetailsModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public DetailsModel(EyewearStoreContext context) => _context = context;

        public class ItemDto
        {
            public int OrderItemId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string SnapshotJson { get; set; } = "";
        }

        public class OrderDto
        {
            public int OrderId { get; set; }
            public string UserFullName { get; set; } = "";
            public string UserEmail { get; set; } = "";
            public string UserPhone { get; set; } = "";
            public string AddressLine { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public string PaymentMethod { get; set; } = "";
            public string TrackingNumber { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        [BindProperty]
        public OrderDto Order { get; set; } = new();

        public List<ItemDto> Items { get; set; } = new();
        public List<string> AllStatuses { get; } = new() { "Pending Confirmation", "Confirmed", "Processing", "Shipped", "Delivered", "Completed", "Cancelled" };

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var o = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.User)
                .Include(x => x.Address)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (o == null) return NotFound();

            Order = new OrderDto
            {
                OrderId = o.OrderId,
                UserFullName = o.User?.FullName ?? "",
                UserEmail = o.User?.Email ?? "",
                UserPhone = o.User?.Phone ?? "",
                AddressLine = o.Address?.AddressLine ?? "",
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod ?? "",
                TrackingNumber = o.Shipments.FirstOrDefault()?.TrackingNumber ?? "",
                CreatedAt = o.CreatedAt
            };

            Items = o.OrderItems.Select(oi => new ItemDto
            {
                OrderItemId = oi.OrderItemId,
                ProductName = oi.Product != null ? oi.Product.Name : "(Deleted product)",
                Sku = oi.Product != null ? oi.Product.Sku : "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                SnapshotJson = oi.SnapshotJson ?? ""
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string newStatus)
        {
            var o = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
            if (o == null) return NotFound();

            var valid = new[] { "Pending Confirmation", "Confirmed", "Processing", "Shipped", "Delivered", "Completed", "Cancelled" };
            if (!valid.Contains(newStatus))
            {
                ModelState.AddModelError("", "Invalid status value");
                return await OnGetAsync(id);
            }

            o.Status = newStatus;
            _context.Orders.Update(o);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order status updated.";
            // TODO: send notification/email as BR requires (call mail service)

            return RedirectToPage(new { id = id });
        }
    }
}
