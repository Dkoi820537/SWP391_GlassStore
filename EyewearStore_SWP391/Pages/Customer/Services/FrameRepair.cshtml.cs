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
                         && (p.InventoryQty == null || p.InventoryQty > 0))
                .OrderBy(p => p.Name).ToListAsync();

            // Lấy tất cả service đang active — admin tạo service "sửa gọng" thì sẽ hiện ở đây
            Services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name).ToListAsync();

            return Page();
        }
    }
}