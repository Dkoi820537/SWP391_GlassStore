using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;
using EyewearStore_SWP391.Services;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Products;

/// <summary>
/// Page model for displaying lens details and handling add to cart functionality.
/// Mirrors the FrameDetailsModel architecture.
/// </summary>
public class LensDetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly ICartService _cartService;

    public LensDetailsModel(EyewearStoreContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    /// <summary>
    /// The lens details to display
    /// </summary>
    public LensDetailsViewModel? Lens { get; set; }

    /// <summary>
    /// Input model for adding to cart
    /// </summary>
    [BindProperty]
    public AddToCartInputModel AddToCartInput { get; set; } = new();

    /// <summary>
    /// Error message to display
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET request - loads lens details by ID
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Query lens from database
        var lens = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == id && l.IsActive);

        if (lens == null)
        {
            return NotFound();
        }

        // Map to LensDetailsViewModel
        Lens = new LensDetailsViewModel
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            Price = lens.Price,
            Currency = lens.Currency,
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            InventoryQty = lens.InventoryQty,
            CreatedAt = lens.CreatedAt,
            CareInstructions = GetCareInstructions(lens.LensType),
            Images = lens.ProductImages
                .OrderBy(i => i.SortOrder)
                .ThenByDescending(i => i.IsPrimary)
                .Select(i => new ProductImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText ?? lens.Name,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                })
                .ToList()
        };

        // Set the product ID for the add to cart input
        AddToCartInput.ProductId = lens.ProductId;
        AddToCartInput.Quantity = 1;

        // Load related lenses (same type or prescription, limit 4)
        Lens.RelatedProducts = await LoadRelatedLensesAsync(lens);

        return Page();
    }

    /// <summary>
    /// Handles POST request - adds lens to cart
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync()
    {
        // Reload the lens for display
        await ReloadLensAsync(AddToCartInput.ProductId);

        if (Lens == null)
        {
            return NotFound();
        }

        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please login to add items to your cart.";
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Products/LensDetails/{AddToCartInput.ProductId}" });
        }

        // Validate quantity
        if (AddToCartInput.Quantity < 1)
        {
            ErrorMessage = "Quantity must be at least 1.";
            return Page();
        }

        // Check if lens is in stock
        if (!Lens.IsInStock)
        {
            ErrorMessage = "Sorry, this lens is currently out of stock.";
            return Page();
        }

        // Check if requested quantity is available
        if (Lens.InventoryQty.HasValue && AddToCartInput.Quantity > Lens.InventoryQty.Value)
        {
            ErrorMessage = $"Sorry, only {Lens.InventoryQty} items are available.";
            return Page();
        }

        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["ErrorMessage"] = "Unable to identify user. Please login again.";
                return RedirectToPage("/Account/Login");
            }

            var userId = int.Parse(userIdClaim.Value);

            // Add to cart using the cart service
            await _cartService.AddToCartAsync(userId, AddToCartInput.ProductId, AddToCartInput.Quantity);

            TempData["SuccessMessage"] = "Lens added to cart!";
            return RedirectToPage("/Products/LensDetails", new { id = AddToCartInput.ProductId });
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = "An error occurred while adding to cart. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// Loads related lenses based on type or prescription
    /// </summary>
    private async Task<List<ProductCatalogItemViewModel>> LoadRelatedLensesAsync(Lens currentLens)
    {
        var relatedLenses = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
            .Where(l => l.IsActive
                && l.ProductId != currentLens.ProductId
                && (l.LensType == currentLens.LensType || l.IsPrescription == currentLens.IsPrescription))
            .OrderByDescending(l => l.CreatedAt)
            .Take(4)
            .AsNoTracking()
            .Select(l => new ProductCatalogItemViewModel
            {
                ProductId = l.ProductId,
                ProductType = "Lens",
                Sku = l.Sku,
                Name = l.Name,
                Description = l.Description,
                Price = l.Price,
                Currency = l.Currency,
                PrimaryImageUrl = l.ProductImages.FirstOrDefault()!.ImageUrl,
                LensType = l.LensType,
                IsPrescription = l.IsPrescription,
                LensIndex = l.LensIndex,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        // If not enough related by type/prescription, fill with other lenses
        if (relatedLenses.Count < 4)
        {
            var existingIds = relatedLenses.Select(r => r.ProductId).ToList();
            existingIds.Add(currentLens.ProductId);

            var additionalLenses = await _context.Lenses
                .Include(l => l.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
                .Where(l => l.IsActive && !existingIds.Contains(l.ProductId))
                .OrderByDescending(l => l.CreatedAt)
                .Take(4 - relatedLenses.Count)
                .AsNoTracking()
                .Select(l => new ProductCatalogItemViewModel
                {
                    ProductId = l.ProductId,
                    ProductType = "Lens",
                    Sku = l.Sku,
                    Name = l.Name,
                    Description = l.Description,
                    Price = l.Price,
                    Currency = l.Currency,
                    PrimaryImageUrl = l.ProductImages.FirstOrDefault()!.ImageUrl,
                    LensType = l.LensType,
                    IsPrescription = l.IsPrescription,
                    LensIndex = l.LensIndex,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            relatedLenses.AddRange(additionalLenses);
        }

        return relatedLenses;
    }

    /// <summary>
    /// Reloads lens data for POST operations
    /// </summary>
    private async Task ReloadLensAsync(int productId)
    {
        var lens = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == productId && l.IsActive);

        if (lens != null)
        {
            Lens = new LensDetailsViewModel
            {
                ProductId = lens.ProductId,
                Sku = lens.Sku,
                Name = lens.Name,
                Description = lens.Description,
                Price = lens.Price,
                Currency = lens.Currency,
                LensType = lens.LensType,
                LensIndex = lens.LensIndex,
                IsPrescription = lens.IsPrescription,
                InventoryQty = lens.InventoryQty,
                CreatedAt = lens.CreatedAt,
                CareInstructions = GetCareInstructions(lens.LensType),
                Images = lens.ProductImages
                    .OrderBy(i => i.SortOrder)
                    .ThenByDescending(i => i.IsPrimary)
                    .Select(i => new ProductImageViewModel
                    {
                        ImageId = i.ImageId,
                        ImageUrl = i.ImageUrl,
                        AltText = i.AltText ?? lens.Name,
                        IsPrimary = i.IsPrimary,
                        SortOrder = i.SortOrder
                    })
                    .ToList()
            };

            Lens.RelatedProducts = await LoadRelatedLensesAsync(lens);
        }
    }

    /// <summary>
    /// Gets care instructions based on lens type
    /// </summary>
    private static string GetCareInstructions(string? lensType)
    {
        return lensType?.ToLower() switch
        {
            "single vision" => "Clean with lens-safe solution and a microfiber cloth. Avoid paper towels or rough fabrics. Store in a protective case when not in use. Handle lenses by the edges.",
            "bifocal" or "progressive" => "Clean gently with lens cleaning solution. Be careful around the transition zones. Store flat in a case to prevent warping. Avoid extreme temperature changes.",
            "photochromic" or "transition" => "Clean with a mild lens cleaner. Allow transition time when moving between indoor and outdoor environments. Avoid leaving in hot cars as heat can affect the photochromic coating.",
            "polarized" => "Use only lens-safe cleaning solution. Avoid ammonia-based cleaners which can damage the polarizing film. Store in a hard case to prevent scratching.",
            _ => "Clean gently with a microfiber cloth and lens cleaning solution. Store in the provided case when not in use. Avoid touching the lens surface with fingers. Keep away from harsh chemicals."
        };
    }
}
