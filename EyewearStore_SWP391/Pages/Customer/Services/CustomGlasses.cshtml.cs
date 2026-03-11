using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class CustomGlassesModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public CustomGlassesModel(EyewearStoreContext context) => _context = context;

        public List<Frame> Frames { get; set; } = new();
        public List<Lens> Lenses { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        /// <summary>
        /// Maps each frameId to its list of compatible lens type strings.
        /// Empty list / missing key = open compatibility (show all lenses).
        /// </summary>
        public Dictionary<int, List<string>> CompatibilityMap { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Frames = await _context.Set<Frame>()
                .Include(f => f.ProductImages)
                .Where(f => f.IsActive && (f.InventoryQty == null || f.InventoryQty > 0))
                .OrderBy(f => f.Name)
                .ToListAsync();

            Lenses = await _context.Set<Lens>()
                .Include(l => l.ProductImages)
                .Where(l => l.IsActive && (l.InventoryQty == null || l.InventoryQty > 0))
                .OrderBy(l => l.Name)
                .ToListAsync();

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

            return Page();
        }
    }
}
