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

        [BindProperty(SupportsGet = true)]
        public int ServicePage { get; set; } = 1;

        private const int ServicePageSize = 6;

        public int TotalServiceCount { get; set; }
        public int TotalServicePages => TotalServiceCount == 0
            ? 0
            : (int)Math.Ceiling(TotalServiceCount / (double)ServicePageSize);

        public int ServicePageStartItem => TotalServiceCount == 0
            ? 0
            : ((ServicePage - 1) * ServicePageSize) + 1;

        public int ServicePageEndItem => TotalServiceCount == 0
            ? 0
            : Math.Min(ServicePage * ServicePageSize, TotalServiceCount);

        public bool HasPreviousServicePage => ServicePage > 1;
        public bool HasNextServicePage => ServicePage < TotalServicePages;

        public async Task<IActionResult> OnGetAsync()
        {
            if (ServicePage < 1)
                ServicePage = 1;

            Frames = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive
                         && p.ProductType.ToLower() == "frame"
                         && (p.InventoryQty == null || p.InventoryQty > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var serviceQuery = _context.Services
                .AsNoTracking()
                .Where(s => s.IsActive &&
                            (s.ServiceCategory == "FrameRepair" || s.ServiceCategory == null))
                .OrderBy(s => s.Name);

            TotalServiceCount = await serviceQuery.CountAsync();

            if (TotalServicePages > 0 && ServicePage > TotalServicePages)
                ServicePage = TotalServicePages;

            Services = await serviceQuery
                .Skip((ServicePage - 1) * ServicePageSize)
                .Take(ServicePageSize)
                .ToListAsync();

            return Page();
        }
    }
}