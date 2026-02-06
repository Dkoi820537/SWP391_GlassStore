using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

/// <summary>
/// Page model for deleting a frame product.
/// Displays confirmation and handles soft delete operation.
/// </summary>
public class DeleteModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DeleteModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The frame view model to display for confirmation
    /// </summary>
    [BindProperty]
    public FrameViewModel Frame { get; set; } = new();

    /// <summary>
    /// Error message to display when deletion fails
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request - loads frame details for confirmation
    /// </summary>
    /// <param name="id">The product ID of the frame to delete</param>
    /// <param name="saveChangesError">Indicates if there was an error during save</param>
    /// <returns>The page or NotFound result</returns>
    public async Task<IActionResult> OnGetAsync(int? id, bool? saveChangesError = false)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query frame from database using AsNoTracking for read-only operation
        var frame = await _context.Frames
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
            TempleLength = frame.TempleLength
        };

        // Set error message if save failed
        if (saveChangesError.GetValueOrDefault())
        {
            ErrorMessage = "Delete failed. Please try again. If the problem persists, the frame may be referenced by other records.";
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request - performs soft delete of the frame
    /// </summary>
    /// <param name="id">The product ID of the frame to delete</param>
    /// <returns>Redirect to Index on success, or Delete page with error</returns>
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Find frame in database
        var frame = await _context.Frames
            .FirstOrDefaultAsync(f => f.ProductId == id);

        // Return NotFound if frame doesn't exist
        if (frame == null)
        {
            return NotFound();
        }

        try
        {
            // Soft Delete: Set IsActive = false and update timestamp
            frame.IsActive = false;
            frame.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Set success message
            TempData["Success"] = $"Frame '{frame.Name}' has been successfully deleted (deactivated).";

            return RedirectToPage("./Index");
        }
        catch (DbUpdateException)
        {
            // On failure, redirect back to Delete page with error flag
            return RedirectToPage("./Delete", new { id, saveChangesError = true });
        }
    }
}
