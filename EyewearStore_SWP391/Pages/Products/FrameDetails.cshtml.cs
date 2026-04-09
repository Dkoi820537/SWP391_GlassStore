using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Products;

public class FrameDetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly ICartService _cartService;

    public FrameDetailsModel(EyewearStoreContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public FrameDetailsViewModel? Frame { get; set; }

    [BindProperty]
    public AddToCartInputModel AddToCartInput { get; set; } = new();

    public string? ErrorMessage { get; set; }

    // ── GET ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var frame = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == id && f.IsActive);

        if (frame == null) return NotFound();

        Frame = await MapToViewModelAsync(frame);
        AddToCartInput.ProductId = frame.ProductId;
        AddToCartInput.Quantity = 1;
        Frame.RelatedProducts = await LoadRelatedAsync(frame);
        return Page();
    }

    // ── POST: Add to cart ─────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddToCartAsync()
    {
        await ReloadAsync(AddToCartInput.ProductId);
        if (Frame == null) return NotFound();

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please log in to add items to your cart.";
            return RedirectToPage("/Account/Login",
                new { returnUrl = $"/Products/FrameDetails/{AddToCartInput.ProductId}" });
        }

        if (AddToCartInput.Quantity < 1) { ErrorMessage = "Quantity must be at least 1."; return Page(); }
        if (!Frame.IsInStock) { ErrorMessage = "This product is out of stock."; return Page(); }
        if (Frame.QuantityOnHand.HasValue && AddToCartInput.Quantity > Frame.QuantityOnHand)
        {
            ErrorMessage = $"Only {Frame.QuantityOnHand} unit(s) available.";
            return Page();
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToPage("/Account/Login");

            await _cartService.AddToCartAsync(int.Parse(userIdClaim.Value),
                AddToCartInput.ProductId, AddToCartInput.Quantity);

            TempData["SuccessMessage"] = "Added to cart!";
            return RedirectToPage("/Products/FrameDetails", new { id = AddToCartInput.ProductId });
        }
        catch (InvalidOperationException ex) { ErrorMessage = ex.Message; return Page(); }
        catch { ErrorMessage = "An error occurred. Please try again."; return Page(); }
    }

    // ── POST: Buy Now (add to cart + redirect to checkout) ──────────────────
    public async Task<IActionResult> OnPostBuyNowAsync()
    {
        await ReloadAsync(AddToCartInput.ProductId);
        if (Frame == null) return NotFound();

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please log in to add items to your cart.";
            return RedirectToPage("/Account/Login",
                new { returnUrl = $"/Products/FrameDetails/{AddToCartInput.ProductId}" });
        }

        if (AddToCartInput.Quantity < 1) { ErrorMessage = "Quantity must be at least 1."; return Page(); }
        if (!Frame.IsInStock) { ErrorMessage = "This product is out of stock."; return Page(); }
        if (Frame.QuantityOnHand.HasValue && AddToCartInput.Quantity > Frame.QuantityOnHand)
        {
            ErrorMessage = $"Only {Frame.QuantityOnHand} unit(s) available.";
            return Page();
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToPage("/Account/Login");

            await _cartService.AddToCartAsync(int.Parse(userIdClaim.Value),
                AddToCartInput.ProductId, AddToCartInput.Quantity);

            return RedirectToPage("/Checkout/Index");
        }
        catch (InvalidOperationException ex) { ErrorMessage = ex.Message; return Page(); }
        catch { ErrorMessage = "An error occurred. Please try again."; return Page(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<FrameDetailsViewModel> MapToViewModelAsync(Frame frame)
    {
        var soldCount = await _context.OrderItems
            .Where(oi => oi.ProductId == frame.ProductId)
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;

        return new FrameDetailsViewModel
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            Price = frame.Price,
            Currency = frame.Currency,
            QuantityOnHand = frame.AvailableStock,
            CreatedAt = frame.CreatedAt,
            CareInstructions = GetCareInstructions(frame.FrameMaterial),
            // Frame specs
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            // v2
            Brand = frame.Brand,
            Color = frame.Color,
            Gender = frame.Gender,
            FrameShape = frame.FrameShape,
            // v3
            LensWidth = frame.LensWidth,
            Origin = frame.Origin,
            // v4
            FrameColor = frame.FrameColor,
            LensMaterial = frame.LensMaterial,
            LensColor = frame.LensColor,
            SuitableFaceShapes = frame.SuitableFaceShapes,
            IsPolarized = frame.IsPolarized,
            HasUvProtection = frame.HasUvProtection,
            StyleTags = frame.StyleTags,
            SoldCount = soldCount,
            Images = frame.ProductImages
                .OrderBy(i => i.SortOrder).ThenByDescending(i => i.IsPrimary)
                .Select(i => new ProductImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText ?? frame.Name,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                }).ToList()
        };
    }

    private async Task ReloadAsync(int productId)
    {
        var frame = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == productId && f.IsActive);

        if (frame != null)
        {
            Frame = await MapToViewModelAsync(frame);
            Frame.RelatedProducts = await LoadRelatedAsync(frame);
        }
    }

    private async Task<List<ProductCatalogItemViewModel>> LoadRelatedAsync(Frame current)
    {
        var related = await _context.Frames
            .Include(f => f.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
            .Where(f => f.IsActive && f.ProductId != current.ProductId
                && (f.Brand == current.Brand
                    || f.FrameMaterial == current.FrameMaterial
                    || f.FrameType == current.FrameType))
            .OrderByDescending(f => f.CreatedAt)
            .Take(4).AsNoTracking()
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
            }).ToListAsync();

        if (related.Count < 4)
        {
            var existingIds = related.Select(r => r.ProductId).Append(current.ProductId).ToList();
            var extra = await _context.Frames
                .Include(f => f.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
                .Where(f => f.IsActive && !existingIds.Contains(f.ProductId))
                .OrderByDescending(f => f.CreatedAt)
                .Take(4 - related.Count).AsNoTracking()
                .Select(f => new ProductCatalogItemViewModel
                {
                    ProductId = f.ProductId,
                    ProductType = "Frame",
                    Sku = f.Sku,
                    Name = f.Name,
                    Price = f.Price,
                    Currency = f.Currency,
                    PrimaryImageUrl = f.ProductImages.FirstOrDefault()!.ImageUrl,
                    FrameMaterial = f.FrameMaterial,
                    FrameType = f.FrameType,
                    CreatedAt = f.CreatedAt
                }).ToListAsync();
            related.AddRange(extra);
        }

        return related;
    }

    private static string GetCareInstructions(string? material) =>
        material?.ToLower() switch
        {
            "metal" or "titanium" =>
                "Clean with a soft lint-free cloth. Avoid harsh chemicals. Store in case when not in use.",
            "plastic" or "acetate" =>
                "Clean with mild soap and water. Avoid alcohol and direct sunlight.",
            "wood" or "bamboo" =>
                "Wipe with a slightly damp cloth. Apply natural oil occasionally.",
            _ => "Clean gently with a microfiber cloth. Store in the provided case."
        };
}
