using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class LensReplacementModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public LensReplacementModel(EyewearStoreContext context) => _context = context;

        public List<Product> Lenses { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Lenses = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && p.ProductType.ToLower() == "lens"
                         && (p.InventoryQty == null || p.InventoryQty > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Chỉ lấy service thuộc LensReplacement hoặc null (hiện ở tất cả)
            Services = await _context.Services
                .Where(s => s.IsActive &&
                       (s.ServiceCategory == "LensReplacement" || s.ServiceCategory == null))
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Page();
        }
    }
}