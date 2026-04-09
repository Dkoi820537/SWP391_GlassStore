using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class FrameRepairModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public FrameRepairModel(EyewearStoreContext context) => _context = context;

        public List<Product> Frames { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Frames = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && p.ProductType.ToLower() == "frame"
                         && (p.QuantityOnHand == null || p.QuantityOnHand > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Chỉ lấy service thuộc FrameRepair hoặc null (hiện ở tất cả)
            Services = await _context.Services
                .Where(s => s.IsActive &&
                       (s.ServiceCategory == "FrameRepair" || s.ServiceCategory == null))
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Page();
        }
    }
}