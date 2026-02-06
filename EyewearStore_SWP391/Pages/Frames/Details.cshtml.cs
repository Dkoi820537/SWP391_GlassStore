using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

/// <summary>
/// Page model for viewing frame product details.
/// Displays complete information about a single frame.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DetailsModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The frame view model containing all frame data
    /// </summary>
    public FrameViewModel Frame { get; set; } = new();

    /// <summary>
    /// Handles GET request - loads frame details from database
    /// </summary>
    /// <param name="id">The product ID of the frame to display</param>
    /// <returns>The page or NotFound result</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query frame from database by ProductId, including images
        var frame = await _context.Frames
            .Include(f => f.ProductImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        // Return NotFound if frame doesn't exist
        if (frame == null)
        {
            return NotFound();
        }

        // Map frame entity to FrameViewModel
        Frame = new FrameViewModel
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            ProductType = frame.ProductType,
            Price = frame.Price,
            Currency = frame.Currency,
            InventoryQty = frame.InventoryQty,
            Attributes = frame.Attributes,
            IsActive = frame.IsActive,
            CreatedAt = frame.CreatedAt,
            UpdatedAt = frame.UpdatedAt,
            // Frame-specific properties
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            // Image properties (only active images)
            PrimaryImageUrl = frame.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? frame.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
            ImageUrls = frame.ProductImages?.Where(i => i.IsActive).OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList() ?? new List<string>()
        };

        return Page();
    }
}
