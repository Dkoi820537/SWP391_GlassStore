using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;
using EyewearStore_SWP391.Services;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Products;

/// <summary>
/// Page model for displaying frame details and handling add to cart functionality.
/// </summary>
public class FrameDetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly ICartService _cartService;

    public FrameDetailsModel(EyewearStoreContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    /// <summary>
    /// The frame details to display
    /// </summary>
    public FrameDetailsViewModel? Frame { get; set; }

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
    /// Handles GET request - loads frame details by ID
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Query frame from database
        var frame = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == id && f.IsActive);

        if (frame == null)
        {
            return NotFound();
        }

        // Map to FrameDetailsViewModel
        Frame = new FrameDetailsViewModel
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            Price = frame.Price,
            Currency = frame.Currency,
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            InventoryQty = frame.InventoryQty,
            CreatedAt = frame.CreatedAt,
            CareInstructions = GetCareInstructions(frame.FrameMaterial),
            Images = frame.ProductImages
                .OrderBy(i => i.SortOrder)
                .ThenByDescending(i => i.IsPrimary)
                .Select(i => new ProductImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText ?? frame.Name,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                })
                .ToList()
        };

        // Set the product ID for the add to cart input
        AddToCartInput.ProductId = frame.ProductId;
        AddToCartInput.Quantity = 1;

        // Load related frames (same material or type, limit 4)
        Frame.RelatedProducts = await LoadRelatedFramesAsync(frame);

        return Page();
    }

    /// <summary>
    /// Handles POST request - adds frame to cart
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync()
    {
        // Reload the frame for display
        await ReloadFrameAsync(AddToCartInput.ProductId);

        if (Frame == null)
        {
            return NotFound();
        }

        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please login to add items to your cart.";
            return RedirectToPage("/Account/Login", new { returnUrl = $"/Products/FrameDetails/{AddToCartInput.ProductId}" });
        }

        // Validate quantity
        if (AddToCartInput.Quantity < 1)
        {
            ErrorMessage = "Quantity must be at least 1.";
            return Page();
        }

        // Check if frame is in stock
        if (!Frame.IsInStock)
        {
            ErrorMessage = "Sorry, this frame is currently out of stock.";
            return Page();
        }

        // Check if requested quantity is available
        if (Frame.InventoryQty.HasValue && AddToCartInput.Quantity > Frame.InventoryQty.Value)
        {
            ErrorMessage = $"Sorry, only {Frame.InventoryQty} items are available.";
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

            TempData["SuccessMessage"] = "Frame added to cart!";
            return RedirectToPage("/Products/FrameDetails", new { id = AddToCartInput.ProductId });
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
    /// Loads related frames based on material or type
    /// </summary>
    private async Task<List<ProductCatalogItemViewModel>> LoadRelatedFramesAsync(Frame currentFrame)
    {
        var relatedFrames = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
            .Where(f => f.IsActive
                && f.ProductId != currentFrame.ProductId
                && (f.FrameMaterial == currentFrame.FrameMaterial || f.FrameType == currentFrame.FrameType))
            .OrderByDescending(f => f.CreatedAt)
            .Take(4)
            .AsNoTracking()
            .Select(f => new ProductCatalogItemViewModel
            {
                ProductId = f.ProductId,
                ProductType = "Frame",
                Sku = f.Sku,
                Name = f.Name,
                Description = f.Description,
                Price = f.Price,
                Currency = f.Currency,
                PrimaryImageUrl = f.ProductImages.FirstOrDefault()!.ImageUrl,
                FrameMaterial = f.FrameMaterial,
                FrameType = f.FrameType,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        // If not enough related by material/type, fill with other frames
        if (relatedFrames.Count < 4)
        {
            var existingIds = relatedFrames.Select(r => r.ProductId).ToList();
            existingIds.Add(currentFrame.ProductId);

            var additionalFrames = await _context.Frames
                .Include(f => f.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
                .Where(f => f.IsActive && !existingIds.Contains(f.ProductId))
                .OrderByDescending(f => f.CreatedAt)
                .Take(4 - relatedFrames.Count)
                .AsNoTracking()
                .Select(f => new ProductCatalogItemViewModel
                {
                    ProductId = f.ProductId,
                    ProductType = "Frame",
                    Sku = f.Sku,
                    Name = f.Name,
                    Description = f.Description,
                    Price = f.Price,
                    Currency = f.Currency,
                    PrimaryImageUrl = f.ProductImages.FirstOrDefault()!.ImageUrl,
                    FrameMaterial = f.FrameMaterial,
                    FrameType = f.FrameType,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            relatedFrames.AddRange(additionalFrames);
        }

        return relatedFrames;
    }

    /// <summary>
    /// Reloads frame data for POST operations
    /// </summary>
    private async Task ReloadFrameAsync(int productId)
    {
        var frame = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == productId && f.IsActive);

        if (frame != null)
        {
            Frame = new FrameDetailsViewModel
            {
                ProductId = frame.ProductId,
                Sku = frame.Sku,
                Name = frame.Name,
                Description = frame.Description,
                Price = frame.Price,
                Currency = frame.Currency,
                FrameMaterial = frame.FrameMaterial,
                FrameType = frame.FrameType,
                BridgeWidth = frame.BridgeWidth,
                TempleLength = frame.TempleLength,
                InventoryQty = frame.InventoryQty,
                CreatedAt = frame.CreatedAt,
                CareInstructions = GetCareInstructions(frame.FrameMaterial),
                Images = frame.ProductImages
                    .OrderBy(i => i.SortOrder)
                    .ThenByDescending(i => i.IsPrimary)
                    .Select(i => new ProductImageViewModel
                    {
                        ImageId = i.ImageId,
                        ImageUrl = i.ImageUrl,
                        AltText = i.AltText ?? frame.Name,
                        IsPrimary = i.IsPrimary,
                        SortOrder = i.SortOrder
                    })
                    .ToList()
            };

            Frame.RelatedProducts = await LoadRelatedFramesAsync(frame);
        }
    }

    /// <summary>
    /// Gets care instructions based on frame material
    /// </summary>
    private static string GetCareInstructions(string? material)
    {
        return material?.ToLower() switch
        {
            "metal" or "titanium" => "Clean with a soft, lint-free cloth. Avoid harsh chemicals. Store in a protective case when not in use. Do not expose to extreme temperatures.",
            "plastic" or "acetate" => "Clean with mild soap and water. Avoid contact with alcohol, acetone, and harsh chemicals. Store away from direct sunlight to prevent discoloration.",
            "wood" or "bamboo" => "Clean with a slightly damp cloth. Apply natural oil occasionally to maintain luster. Keep away from excessive moisture.",
            _ => "Clean gently with a microfiber cloth. Store in the provided case when not in use. Avoid dropping or sitting on your eyewear."
        };
    }
}
