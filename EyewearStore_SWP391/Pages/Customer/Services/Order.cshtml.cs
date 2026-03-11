using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class OrderModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public OrderModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public List<Product> Frames { get; set; } = new();
        public List<Product> Lenses { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        /// <summary>
        /// Maps each frameId to its list of compatible lens type strings.
        /// </summary>
        public Dictionary<int, List<string>> CompatibilityMap { get; set; } = new();

        /// <summary>
        /// Maps lens ProductId → LensType string (needed because Lenses list uses Product base type).
        /// </summary>
        public Dictionary<int, string> LensTypeMap { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Load Frames
            Frames = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive
                         && p.ProductType.ToLower() == "frame"
                         && (p.InventoryQty == null || p.InventoryQty > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Load Lenses
            Lenses = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive
                         && p.ProductType.ToLower() == "lens"
                         && (p.InventoryQty == null || p.InventoryQty > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Load active Services (loại gia công)
            Services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Load frame → compatible lens types mapping
            var frameIds = Frames.Select(f => f.ProductId).ToList();
            CompatibilityMap = (await _context.FrameCompatibleLensTypes
                .Where(c => frameIds.Contains(c.FrameProductId))
                .ToListAsync())
                .GroupBy(c => c.FrameProductId)
                .ToDictionary(g => g.Key, g => g.Select(c => c.LensType).ToList());

            // Build lens ProductId → LensType lookup
            var lensIds = Lenses.Select(l => l.ProductId).ToList();
            LensTypeMap = await _context.Lenses
                .Where(l => lensIds.Contains(l.ProductId))
                .ToDictionaryAsync(l => l.ProductId, l => l.LensType ?? "");

            return Page();
        }
    }
}
