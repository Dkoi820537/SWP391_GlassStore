using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;

namespace EyewearStore_SWP391.Pages.Products;

/// <summary>
/// Page model for displaying product details.
/// Handles both Frame and Lens products.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DetailsModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The product details to display
    /// </summary>
    public ProductCatalogItemViewModel? Product { get; set; }

    /// <summary>
    /// List of product images
    /// </summary>
    public List<ProductImage> ProductImages { get; set; } = new();

    /// <summary>
    /// The product type (Frame or Lens)
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = "Frame";

    /// <summary>
    /// Handles GET request - loads product details by ID
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (Type == "Frame")
        {
            var frame = await _context.Frames
                .Include(f => f.ProductImages.Where(pi => pi.IsActive))
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ProductId == id && f.IsActive);

            if (frame == null)
            {
                return NotFound();
            }

            Product = new ProductCatalogItemViewModel
            {
                ProductId = frame.ProductId,
                ProductType = "Frame",
                Sku = frame.Sku,
                Name = frame.Name,
                Description = frame.Description,
                Price = frame.Price,
                Currency = frame.Currency,
                PrimaryImageUrl = frame.ProductImages?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? frame.ProductImages?.FirstOrDefault()?.ImageUrl,
                FrameMaterial = frame.FrameMaterial,
                FrameType = frame.FrameType,
                CreatedAt = frame.CreatedAt
            };

            ProductImages = frame.ProductImages?.OrderBy(i => i.SortOrder).ToList() ?? new List<ProductImage>();
        }
        else if (Type == "Lens")
        {
            var lens = await _context.Lenses
                .Include(l => l.ProductImages.Where(pi => pi.IsActive))
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ProductId == id && l.IsActive);

            if (lens == null)
            {
                return NotFound();
            }

            Product = new ProductCatalogItemViewModel
            {
                ProductId = lens.ProductId,
                ProductType = "Lens",
                Sku = lens.Sku,
                Name = lens.Name,
                Description = lens.Description,
                Price = lens.Price,
                Currency = lens.Currency,
                PrimaryImageUrl = lens.ProductImages?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? lens.ProductImages?.FirstOrDefault()?.ImageUrl,
                LensType = lens.LensType,
                LensIndex = lens.LensIndex,
                IsPrescription = lens.IsPrescription,
                CreatedAt = lens.CreatedAt
            };

            ProductImages = lens.ProductImages?.OrderBy(i => i.SortOrder).ToList() ?? new List<ProductImage>();
        }
        else
        {
            return NotFound();
        }

        return Page();
    }
}
