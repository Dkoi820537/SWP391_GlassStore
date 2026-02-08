using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

/// <summary>
/// Page model for deleting a lens product.
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
    /// The lens view model to display for confirmation
    /// </summary>
    [BindProperty]
    public LensViewModel Lens { get; set; } = new();

    /// <summary>
    /// Error message to display when deletion fails
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request - loads lens details for confirmation
    /// </summary>
    /// <param name="id">The product ID of the lens to delete</param>
    /// <param name="saveChangesError">Indicates if there was an error during save</param>
    /// <returns>The page or NotFound result</returns>
    public async Task<IActionResult> OnGetAsync(int? id, bool? saveChangesError = false)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query lens from database using AsNoTracking for read-only operation
        var lens = await _context.Lenses
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
            IsPrescription = lens.IsPrescription
        };

        // Set error message if save failed
        if (saveChangesError.GetValueOrDefault())
        {
            ErrorMessage = "Delete failed. Please try again. If the problem persists, the lens may be referenced by other records.";
        }

        return Page();
    }

    /// <summary>
    /// Handles POST request - performs soft delete of the lens
    /// </summary>
    /// <param name="id">The product ID of the lens to delete</param>
    /// <returns>Redirect to Index on success, or Delete page with error</returns>
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Find lens in database
        var lens = await _context.Lenses
            .FirstOrDefaultAsync(l => l.ProductId == id);

        // Return NotFound if lens doesn't exist
        if (lens == null)
        {
            return NotFound();
        }

        try
        {
            // Soft Delete: Set IsActive = false and update timestamp
            lens.IsActive = false;
            lens.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Set success message
            TempData["Success"] = $"Lens '{lens.Name}' has been successfully deleted (deactivated).";

            return RedirectToPage("./Index");
        }
        catch (DbUpdateException)
        {
            // On failure, redirect back to Delete page with error flag
            return RedirectToPage("./Delete", new { id, saveChangesError = true });
        }
    }
}
