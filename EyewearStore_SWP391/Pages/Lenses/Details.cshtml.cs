using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

/// <summary>
/// Page model for viewing lens product details.
/// Displays comprehensive information about a single lens.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DetailsModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The lens view model to display
    /// </summary>
    public LensViewModel Lens { get; set; } = new();

    /// <summary>
    /// List of product images
    /// </summary>
    public List<ProductImage> ProductImages { get; set; } = new();

    /// <summary>
    /// Handles GET request - loads lens details
    /// </summary>
    /// <param name="id">The product ID of the lens to view</param>
    /// <returns>The page or NotFound result</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query lens from database with images
        var lens = await _context.Lenses
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        // Return NotFound if lens doesn't exist
        if (lens == null)
        {
            return NotFound();
        }

        // Get product images
        ProductImages = await _context.ProductImages
            .Where(pi => pi.ProductId == id && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.SortOrder)
            .ToListAsync();

        // Get primary image URL
        var primaryImageUrl = ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageUrl
            ?? ProductImages.FirstOrDefault()?.ImageUrl;

        // Map lens entity to LensViewModel
        Lens = new LensViewModel
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            ProductType = lens.ProductType,
            Price = lens.Price,
            Currency = lens.Currency,
            InventoryQty = lens.InventoryQty,
            Attributes = lens.Attributes,
            IsActive = lens.IsActive,
            CreatedAt = lens.CreatedAt,
            UpdatedAt = lens.UpdatedAt,
            // Lens-specific properties
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            // Images
            PrimaryImageUrl = primaryImageUrl,
            ImageUrls = ProductImages.Select(pi => pi.ImageUrl).ToList()
        };

        return Page();
    }
}
