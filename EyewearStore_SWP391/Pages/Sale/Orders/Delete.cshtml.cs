using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class DeleteModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DeleteModel(EyewearStoreContext context) => _context = context;

        [BindProperty]
        public int OrderId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            OrderId = id;

            var exists = await _context.Orders.AnyAsync(o => o.OrderId == id);

            if (!exists)
            {
                TempData["Error"] = $"Order #{id} not found";
                return RedirectToPage("Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);

            if (order == null)
            {
                TempData["Error"] = $"Order #{OrderId} not found";
                return RedirectToPage("Index");
            }

            // BR: Prevent deletion of completed or shipped orders (optional business rule)
            if (order.Status == "Completed" || order.Status == "Shipped" || order.Status == "Delivered")
            {
                TempData["Error"] = $"Cannot delete order #{OrderId} with status '{order.Status}'. Only pending or cancelled orders can be deleted.";
                return RedirectToPage("Details", new { id = OrderId });
            }

            // BR: Restore inventory before deleting (if order wasn't cancelled)
            if (order.Status != "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null && item.Product.InventoryQty.HasValue)
                    {
                        item.Product.InventoryQty += item.Quantity;
                        _context.Products.Update(item.Product);
                    }
                }
            }

            // Delete related shipments
            var shipments = await _context.Shipments
                .Where(s => s.OrderId == OrderId)
                .ToListAsync();

            if (shipments.Any())
            {
                _context.Shipments.RemoveRange(shipments);
            }

            // Delete order items
            if (order.OrderItems.Any())
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
            }

            // Delete the order
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{OrderId} has been deleted successfully";
            return RedirectToPage("Index");
        }
    }
}
