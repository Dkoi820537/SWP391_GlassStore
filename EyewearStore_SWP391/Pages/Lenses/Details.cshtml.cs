using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

/// <summary>
/// Page model for viewing lens product details.
/// Displays complete information about a single lens.
/// Mirrors the Frames/Details architecture.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DetailsModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The lens view model containing all lens data
    /// </summary>
    public LensViewModel Lens { get; set; } = new();

    /// <summary>
    /// Handles GET request - loads lens details from database
    /// </summary>
    /// <param name="id">The product ID of the lens to display</param>
    /// <returns>The page or NotFound result</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query lens from database by ProductId, including images
        var lens = await _context.Lenses
            .Include(l => l.ProductImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        // Return NotFound if lens doesn't exist
        if (lens == null)
        {
            return NotFound();
        }

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
            // Image properties (only active images)
            PrimaryImageUrl = lens.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? lens.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
            ImageUrls = lens.ProductImages?.Where(i => i.IsActive).OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList() ?? new List<string>()
        };

        return Page();
    }
}
