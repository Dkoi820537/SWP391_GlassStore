using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;
using EyewearStore_SWP391.Services;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Products;

public class LensDetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly ICartService _cartService;

    public LensDetailsModel(EyewearStoreContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public LensDetailsViewModel? Lens { get; set; }

    [BindProperty]
    public AddToCartInputModel AddToCartInput { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public List<PrescriptionProfile> Prescriptions { get; set; } = new();

    // ── GET ────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var lens = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == id && l.IsActive);

        if (lens == null) return NotFound();

        Lens = await MapToViewModelAsync(lens);
        AddToCartInput.ProductId = lens.ProductId;
        AddToCartInput.Quantity = 1;
        Lens.RelatedProducts = await LoadRelatedAsync(lens);
        await LoadPrescriptionsAsync();
        return Page();
    }

    // ── POST: Add to cart ──────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddToCartAsync()
    {
        await ReloadAsync(AddToCartInput.ProductId);
        if (Lens == null) return NotFound();

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please login to add items to your cart.";
            return RedirectToPage("/Account/Login",
                new { returnUrl = $"/Products/LensDetails/{AddToCartInput.ProductId}" });
        }

        if (AddToCartInput.Quantity < 1) { ErrorMessage = "Quantity must be at least 1."; return Page(); }
        if (!Lens.IsInStock) { ErrorMessage = "Sorry, this lens is out of stock."; return Page(); }
        if (Lens.InventoryQty.HasValue && AddToCartInput.Quantity > Lens.InventoryQty.Value)
        {
            ErrorMessage = $"Only {Lens.InventoryQty} items available.";
            return Page();
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToPage("/Account/Login");

            await _cartService.AddToCartAsync(
                int.Parse(userIdClaim.Value),
                AddToCartInput.ProductId,
                AddToCartInput.Quantity,
                prescriptionId: AddToCartInput.PrescriptionId);

            TempData["SuccessMessage"] = "Lens added to cart!";
            return RedirectToPage("/Products/LensDetails", new { id = AddToCartInput.ProductId });
        }
        catch (InvalidOperationException ex) { ErrorMessage = ex.Message; return Page(); }
        catch { ErrorMessage = "An error occurred. Please try again."; return Page(); }
    }

    // ── POST: Buy Now (add to cart + redirect to checkout) ──────────────────
    public async Task<IActionResult> OnPostBuyNowAsync()
    {
        await ReloadAsync(AddToCartInput.ProductId);
        if (Lens == null) return NotFound();

        if (!User.Identity?.IsAuthenticated ?? true)
        {
            TempData["ErrorMessage"] = "Please login to add items to your cart.";
            return RedirectToPage("/Account/Login",
                new { returnUrl = $"/Products/LensDetails/{AddToCartInput.ProductId}" });
        }

        if (AddToCartInput.Quantity < 1) { ErrorMessage = "Quantity must be at least 1."; return Page(); }
        if (!Lens.IsInStock) { ErrorMessage = "Sorry, this lens is out of stock."; return Page(); }
        if (Lens.InventoryQty.HasValue && AddToCartInput.Quantity > Lens.InventoryQty.Value)
        {
            ErrorMessage = $"Only {Lens.InventoryQty} items available.";
            return Page();
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToPage("/Account/Login");

            await _cartService.AddToCartAsync(
                int.Parse(userIdClaim.Value),
                AddToCartInput.ProductId,
                AddToCartInput.Quantity,
                prescriptionId: AddToCartInput.PrescriptionId);

            return RedirectToPage("/Checkout/Index");
        }
        catch (InvalidOperationException ex) { ErrorMessage = ex.Message; return Page(); }
        catch { ErrorMessage = "An error occurred. Please try again."; return Page(); }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<LensDetailsViewModel> MapToViewModelAsync(Lens lens)
    {
        var soldCount = await _context.OrderItems
            .Where(oi => oi.ProductId == lens.ProductId)
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;

        return new LensDetailsViewModel
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            Price = lens.Price,
            Currency = lens.Currency,
            InventoryQty = lens.InventoryQty,
            CreatedAt = lens.CreatedAt,
            // Brand & identity
            Brand = lens.Brand,
            Origin = lens.Origin,
            // Lens specs
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            PrescriptionFee = lens.IsPrescription ? (lens.PrescriptionFee ?? 500_000m) : 0m,
            LensMaterial = lens.LensMaterial,
            LensThickness = lens.LensThickness,
            // Coatings
            LensCoating = lens.LensCoating,
            UVProtection = lens.UVProtection,
            SoldCount = soldCount,
            CareInstructions = GetCareInstructions(lens.LensType),
            Images = lens.ProductImages
                .OrderBy(i => i.SortOrder).ThenByDescending(i => i.IsPrimary)
                .Select(i => new ProductImageViewModel
                {
                    ImageId = i.ImageId,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText ?? lens.Name,
                    IsPrimary = i.IsPrimary,
                    SortOrder = i.SortOrder
                }).ToList()
        };
    }

    private async Task ReloadAsync(int productId)
    {
        var lens = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == productId && l.IsActive);

        if (lens != null)
        {
            Lens = await MapToViewModelAsync(lens);
            Lens.RelatedProducts = await LoadRelatedAsync(lens);
            await LoadPrescriptionsAsync();
        }
    }

    private async Task LoadPrescriptionsAsync()
    {
        if (User?.Identity?.IsAuthenticated != true) return;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) return;
        Prescriptions = await _context.PrescriptionProfiles
            .Where(p => p.UserId == int.Parse(claim.Value) && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    private async Task<List<ProductCatalogItemViewModel>> LoadRelatedAsync(Lens current)
    {
        var related = await _context.Lenses
            .Include(l => l.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
            .Where(l => l.IsActive && l.ProductId != current.ProductId
                && (l.LensType == current.LensType
                    || l.Brand == current.Brand
                    || l.IsPrescription == current.IsPrescription))
            .OrderByDescending(l => l.CreatedAt)
            .Take(4).AsNoTracking()
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
            }).ToListAsync();

        if (related.Count < 4)
        {
            var existingIds = related.Select(r => r.ProductId).Append(current.ProductId).ToList();
            var extra = await _context.Lenses
                .Include(l => l.ProductImages.Where(pi => pi.IsActive && pi.IsPrimary))
                .Where(l => l.IsActive && !existingIds.Contains(l.ProductId))
                .OrderByDescending(l => l.CreatedAt)
                .Take(4 - related.Count).AsNoTracking()
                .Select(l => new ProductCatalogItemViewModel
                {
                    ProductId = l.ProductId,
                    ProductType = "Lens",
                    Sku = l.Sku,
                    Name = l.Name,
                    Price = l.Price,
                    Currency = l.Currency,
                    PrimaryImageUrl = l.ProductImages.FirstOrDefault()!.ImageUrl,
                    LensType = l.LensType,
                    IsPrescription = l.IsPrescription,
                    LensIndex = l.LensIndex,
                    CreatedAt = l.CreatedAt
                }).ToListAsync();
            related.AddRange(extra);
        }
        return related;
    }

    private static string GetCareInstructions(string? lensType) =>
        lensType?.ToLower() switch
        {
            "single vision" =>
                "Clean with lens-safe solution and a microfiber cloth. Avoid paper towels. Store in a protective case. Handle by the edges.",
            "bifocal" or "progressive" =>
                "Clean gently with lens cleaning solution. Be careful around transition zones. Store flat in a case. Avoid extreme temperature changes.",
            "photochromic" or "transition" =>
                "Clean with a mild lens cleaner. Allow transition time between indoor and outdoor. Avoid leaving in hot cars.",
            "polarized" =>
                "Use only lens-safe solution. Avoid ammonia-based cleaners. Store in a hard case to prevent scratching.",
            _ =>
                "Clean gently with a microfiber cloth and lens cleaning solution. Store in the provided case when not in use. Avoid touching the lens surface with fingers. Keep away from harsh chemicals."
        };
}
